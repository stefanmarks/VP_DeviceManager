using Common.Logging;
using Common.Logging.Configuration;
using SentienceLab.OSC;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO.Ports;
using System.Text;
using UnityEngine;
using XBeeLibrary.Core;
using XBeeLibrary.Core.Events;
using XBeeLibrary.Core.IO;
using XBeeLibrary.Core.Models;
using XBeeLibrary.Windows.Connection.Serial;

[AddComponentMenu("VP/XBee Manager")]
public class XBeeManager : MonoBehaviour
{
	public enum EDeviceType {
		[InspectorName("XBee (v1)")]   XBee,
		[InspectorName("ZigBee (v2)")] ZigBee 
	};

	[Header("Device")]
	public EDeviceType DeviceType       = EDeviceType.XBee;
	public int         DiscoveryTimeout = 10000;

	[Header("Serial Connection")]
	public string    COM_Port       = "COM1";
	public int       Baudrate       = 57600;
	public StopBits  StopBits       = StopBits.None;
	public Parity    Parity         = Parity.None;
	public Handshake Handshake      = Handshake.None;
	public int       ReceiveTimeout = 100;

	[Header("OSC")]
	public string OSC_Prefix            = "/";
	public float  MinimumUpdateInterval = 1.0f;


	public void Start()
	{
		LogConfiguration config = new LogConfiguration();
		config.FactoryAdapter = new FactoryAdapterConfiguration();
		config.FactoryAdapter.Type = typeof(Common.Logging.Simple.UnityDebugLoggerFactoryAdapter).ToString();
		config.FactoryAdapter.Arguments = new NameValueCollection {
			{ "level", "Info" }
		};

		LogManager.Configure(config);

		SerialPortParameters serialParams = new SerialPortParameters(Baudrate, 8, StopBits, Parity, Handshake);
		m_serialPort = new WinSerialPort(COM_Port, serialParams, ReceiveTimeout);
		m_device     = null;

		m_remoteDevices = new Dictionary<XBee64BitAddress, RemoteXBeeDevice>();
		m_newDevices    = new List<RemoteXBeeDevice>();
		m_oscVariables  = new Dictionary<string, OSC_BoolVariable>();
		m_nextIO_Update = 0;

		StartCoroutine(OpenCoordinator());
	}


	public void OnDestroy()
	{
		if ((m_device != null) && m_device.IsOpen)
		{
			m_device.Close();
		}
	}


	protected IEnumerator OpenCoordinator()
	{
		try
		{
			Debug.Log("Contacting XBee Coordinator");

			switch (DeviceType)
			{
				case EDeviceType.XBee   : m_device = new Raw802Device(m_serialPort); break;
				case EDeviceType.ZigBee : m_device = new ZigBeeDevice(m_serialPort); break;
			}
			
			m_device.Open();
			Debug.Log($"Opened XBee Coordinator {DeviceInfo(m_device)}");

			m_device.IOSampleReceived += OnIOSampleReceived;

			Debug.Log("Starting XBee network discovery");
			m_network = m_device.GetNetwork();
			m_network.SetDiscoveryTimeout(DiscoveryTimeout);
			m_network.DeviceDiscovered += OnDeviceDiscovered;
			m_network.StartNodeDiscoveryProcess();
		}
		catch (Exception e)
		{
			Debug.LogWarning($"Could not open XBee Coordinator ({e})");
		}

		if (m_network != null)
		{
			while (m_network.IsDiscoveryRunning)
			{
				yield return new WaitForSeconds(1);
			}

			Debug.Log("XBee network discovery ended");
		}

		yield return null;
	}

	protected void OnIOSampleReceived(object sender, IOSampleReceivedEventArgs args)
	{
		RemoteXBeeDevice device = args.RemoteDevice;
		if (device != null)
		{
			MaintainRemoteDevices(device);

			StringBuilder sb = new StringBuilder();
			sb.Append("IO Sample from ").Append(DeviceInfo(device)).Append(":");
			foreach (var kv in args.IOSample.DigitalValues)
			{
				sb.Append($"\n- {kv.Key}={kv.Value}");
			}
			Debug.Log(sb);

			UpdateOSC_Variables(device, args.IOSample);
		}
	}

	protected void OnDeviceDiscovered(object sender, DeviceDiscoveredEventArgs args)
	{
		RemoteXBeeDevice device = args.DiscoveredDevice;
		if (device != null)
		{
			MaintainRemoteDevices(device);
		}
	}

	protected void MaintainRemoteDevices(RemoteXBeeDevice device)
	{
		if (!m_remoteDevices.ContainsKey(device.XBee64BitAddr))
		{
			Debug.Log($"Found new {DeviceInfo(device)}");
			m_remoteDevices[device.XBee64BitAddr] = device;
			m_newDevices.Add(device);
		}
	}

	protected IEnumerator ReadDeviceStatus(RemoteXBeeDevice device)
	{
		yield return new WaitForSeconds(0.1f);

		if ((device.NodeID == null) || (device.NodeID.Length == 0))
		{
			Debug.Log($"Reading info of {DeviceInfo(device)}");
			device.ReadDeviceInfo();
		}

		Debug.Log($"Reading IO state of {DeviceInfo(device)}");
		IOSample ioSample = device.ReadIOSample();
		UpdateOSC_Variables(device, ioSample);
		StringBuilder sb = new StringBuilder();
		foreach (var kv in ioSample.DigitalValues)
		{
			sb.Append($"\n- {kv.Key}={kv.Value}");
		}
		Debug.Log(sb);
	}

	protected string DeviceInfo(AbstractXBeeDevice device)
	{
		StringBuilder sb = new StringBuilder(device.GetType().Name);
		sb.Append(" ");
		if ((device.NodeID != null) && (device.NodeID.Length > 0))
		{
			sb.Append("'").Append(device.NodeID).Append("'/");
		}
		sb.Append(device.XBee64BitAddr);
		return sb.ToString();
	}

	public void UpdateOSC_Variables(AbstractXBeeDevice device, IOSample sample)
	{
		if ((device.NodeID == null) || (device.NodeID.Length == 0)) return;

		string prefix = device.NodeID.ToLower().Replace(" ", "_");
		foreach (var kv in sample.DigitalValues)
		{
			string varName = kv.Key.GetName().ToLower();
			
			// cut analog name part (if exists)
			int analogNameStartIdx = varName.IndexOf("/ad");
			if (analogNameStartIdx >= 0)
			{
				varName = varName.Substring(0, analogNameStartIdx);
			}

			// build full name
			varName = OSC_Prefix + prefix + "/" + varName.Replace("dio", "input"); ;
			
			// search or create OSC variable
			OSC_BoolVariable oscVar = null;
			if (!m_oscVariables.TryGetValue(varName, out oscVar))
			{
				oscVar = new OSC_BoolVariable(varName);
				m_oscVariables.Add(varName, oscVar);
			}

			// set new value
			bool newValue = kv.Value == IOValue.LOW;
			if (oscVar.Value != newValue)
			{
				oscVar.Value = newValue;
				oscVar.SendUpdate();
			}
		}
	}

	public void Update()
	{
		// query new devices
		if (m_newDevices.Count > 0)
		{
			foreach (var device in m_newDevices) 
			{
				StartCoroutine(ReadDeviceStatus(device));
			}
			m_newDevices.Clear();
		}

		// force send IO updates in intervals
		m_nextIO_Update -= Time.deltaTime;
		bool doUpdate = m_nextIO_Update <= 0;
		if (doUpdate)
		{
			foreach (var kv in m_oscVariables)
			{
				kv.Value.SendUpdate();
			}
			m_nextIO_Update = MinimumUpdateInterval;
		}
	}


	protected WinSerialPort m_serialPort;
	protected XBeeDevice    m_device;
	protected XBeeNetwork   m_network;

	protected List<RemoteXBeeDevice> m_newDevices;

	protected Dictionary<XBee64BitAddress, RemoteXBeeDevice> m_remoteDevices;
	protected Dictionary<string, OSC_BoolVariable>           m_oscVariables;
	protected double                                         m_nextIO_Update;
}
