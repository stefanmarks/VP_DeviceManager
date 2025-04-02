using Common.Logging;
using Common.Logging.Configuration;
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
	public string    COM_Port       = "COM13";
	public int       Baudrate       = 57600;
	public StopBits  StopBits       = StopBits.None;
	public Parity    Parity         = Parity.None;
	public Handshake Handshake      = Handshake.None;
	public int       ReceiveTimeout = 100;

	public int       DiscoveryTimeout = 10000;

	public void Start()
	{
		LogConfiguration config = new LogConfiguration();
		config.FactoryAdapter = new FactoryAdapterConfiguration();
		config.FactoryAdapter.Type = typeof(Common.Logging.Simple.UnityDebugLoggerFactoryAdapter).ToString();
		config.FactoryAdapter.Arguments = new NameValueCollection {
			{ "level", "Info" }
		};

		Debug.Log(JsonUtility.ToJson(config));
		LogManager.Configure(config);

		SerialPortParameters serialParams = new SerialPortParameters(Baudrate, 8, StopBits, Parity, Handshake);
		m_serialPort = new WinSerialPort(COM_Port, serialParams, ReceiveTimeout);
		m_device     = new ZigBeeDevice(m_serialPort);

		m_remoteDevices = new Dictionary<XBee64BitAddress, RemoteXBeeDevice>();
		m_newDevices    = new List<RemoteXBeeDevice>();

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
		Debug.Log("Contacting XBee Coordinator");

		try
		{
			m_device.Open();
			Debug.Log($"Opened XBee Coordinator '{m_device.NodeID}', Address {m_device.XBee64BitAddr}");

			m_device.IOSampleReceived += OnIOSampleReceived;

			Debug.Log("Starting XBee network discovery");
			m_network = m_device.GetNetwork();
			m_network.SetDiscoveryTimeout(DiscoveryTimeout);
			m_network.DeviceDiscovered += OnDeviceDiscovered;
			m_network.StartNodeDiscoveryProcess();
		}
		catch (Exception)
		{
			Debug.LogWarning("Could not open XBee Coordinator");
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
			sb.Append($"IO Sample from {device.NodeID}/{device.XBee64BitAddr}:");
			foreach (var kv in args.IOSample.DigitalValues)
			{
				sb.Append($"\n- {kv.Key}={kv.Value}");
			}
			Debug.Log(sb);
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
			m_remoteDevices[device.XBee64BitAddr] = device;
			Debug.Log($"Found new XBee device {device.NodeID}/{device.XBee64BitAddr}");
			m_newDevices.Add(device);
		}
	}

	protected IEnumerator ReadDeviceStatus(RemoteXBeeDevice device)
	{
		yield return null;

		Debug.Log("Reading XBee device");
		StringBuilder sb = new StringBuilder();
		IOSample ioSample = device.ReadIOSample();
		foreach (var kv in ioSample.DigitalValues)
		{
			sb.Append($"\n- {kv.Key}={kv.Value}");
		}
		Debug.Log(sb);
	}

	public void Update()
	{
		if (m_newDevices.Count > 0)
		{
			foreach (var device in m_newDevices) {
				StartCoroutine(ReadDeviceStatus(device));
			}
			m_newDevices.Clear();
		}
	}


	protected WinSerialPort m_serialPort;
	protected XBeeDevice    m_device;
	protected XBeeNetwork   m_network;

	protected List<RemoteXBeeDevice> m_newDevices;

	protected Dictionary<XBee64BitAddress, RemoteXBeeDevice> m_remoteDevices;
}
