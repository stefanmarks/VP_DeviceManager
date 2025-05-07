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
using XBeeLibrary.Core.Utils;
using XBeeLibrary.Windows.Connection.Serial;

[AddComponentMenu("VP/XBee Manager")]
public class XBeeManager : MonoBehaviour, IDeviceManager, IDevice
{
	public enum EDeviceType {
		[InspectorName("XBee v1")] XBee1,
		[InspectorName("XBee v2")] XBee2,
		[InspectorName("XBee v3")] XBee3
	};

	[Header("Device")]
	public EDeviceType DeviceType       = EDeviceType.XBee3;

	[Tooltip("XBee node discovery timeout in [s]")]
	[Min(1)]
	public float       DiscoveryTimeout = 60;

	[Header("Serial Connection")]
	public string    COM_Port       = "COM1";
	public int       Baudrate       = 115200;
	public StopBits  StopBits       = StopBits.None;
	public Parity    Parity         = Parity.None;
	public Handshake Handshake      = Handshake.None;
	[Tooltip("Serial communication receive timeout in [ms]")]
	[Min(1)]
	public int       ReceiveTimeout = AbstractXBeeDevice.DEFAULT_RECEIVE_TIMEOUT;

	[Header("OSC")]
	public string OSC_Prefix            = "/";
	[Tooltip("Minimum OSC variable update interval in [s]")]
	[Min(0)]
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
		m_serialPort  = new WinSerialPort(COM_Port, serialParams, ReceiveTimeout);
		m_coordinator = null;

		m_remoteDevices = new Dictionary<XBee64BitAddress, XBee>();
		m_newDevices    = new List<XBee>();
		m_nextIO_Update = 0;

		StartCoroutine(OpenCoordinator());
	}


	public void OnDestroy()
	{
		if ((m_coordinator != null) && m_coordinator.IsOpen)
		{
			m_coordinator.Close();
		}
	}


	protected IEnumerator OpenCoordinator()
	{
		try
		{
			Debug.Log("Contacting XBee Coordinator");

			switch (DeviceType)
			{
				case EDeviceType.XBee1 : m_coordinator = new Raw802Device(m_serialPort); break;
				case EDeviceType.XBee2 : // fallthrough
				case EDeviceType.XBee3 : m_coordinator = new ZigBeeDevice(m_serialPort); break;
			}
			m_coordinator.ReceiveTimeout = ReceiveTimeout;
			m_coordinator.Open();

			Debug.Log($"Opened XBee Coordinator {DeviceInfo(m_coordinator)}");
		}
		catch (Exception e)
		{
			Debug.LogWarning($"Could not open XBee Coordinator ({e})");
		}

		if (m_coordinator.IsInitialized)
		{
			m_coordinator.IOSampleReceived += OnIOSampleReceived;
			/*
			if (DeviceType == EDeviceType.XBee3)
			{
				// XBee3: open Join Window by virtually pushing the commissioning button twice
				var response = m_coordinator.SendPacket(new ATCommandPacket(m_coordinator.GetNextFrameID(), "CB", "2"));
				if (response is ATCommandResponsePacket atResponse)
				{
					if (atResponse.Status == ATCommandStatus.OK)
					{
						Debug.Log("Opening join window");
					}
					else
					{
						Debug.LogWarning("Could not open join window");
					}
				}
				yield return new WaitForSeconds(1);
			}
			*/
			Debug.Log("Starting XBee network discovery");
			
			if (DeviceType == EDeviceType.XBee3)
			{
				// change NodeJoinTime (in s)
				m_coordinator.SetParameter("NJ", ByteUtils.LongToByteArray((long)(DiscoveryTimeout / 1.0f)));
			}

			m_network = m_coordinator.GetNetwork();
			if (m_network != null)
			{
				m_network.SetDiscoveryTimeout((long)(DiscoveryTimeout * 1000.0f));
				m_network.DeviceDiscovered += OnDeviceDiscovered;
				m_network.StartNodeDiscoveryProcess();

				while (m_network.IsDiscoveryRunning)
				{
					yield return new WaitForSeconds(1);
				}

				Debug.Log("XBee network discovery ended");
			}
		}
	}

	protected void OnIOSampleReceived(object sender, IOSampleReceivedEventArgs args)
	{
		RemoteXBeeDevice device = args.RemoteDevice;
		if (device != null)
		{
			MaintainRemoteDevices(device);

			//StringBuilder sb = new StringBuilder();
			//sb.Append("IO Sample from ").Append(DeviceInfo(device)).Append(":");
			//foreach (var kv in args.IOSample.DigitalValues)
			//{
			//	sb.Append($"\n- {kv.Key}={kv.Value}");
			//}
			//Debug.Log(sb);

			ProcessIOSample(device, args.IOSample);
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
			XBee xbee = new XBee(this, device);
			m_remoteDevices[device.XBee64BitAddr] = xbee;
			m_newDevices.Add(xbee);
		}
	}


	public void ProcessIOSample(AbstractXBeeDevice device, IOSample sample)
	{
		if ((device.NodeID == null) || (device.NodeID.Length == 0)) return;

		if (m_remoteDevices.TryGetValue(device.XBee64BitAddr, out XBee xbee))
		{
			xbee.ProcessIOSample(sample);
		}
	}


	public void Update()
	{
		// query new devices
		if (m_newDevices.Count > 0)
		{
			foreach (var device in m_newDevices) 
			{
				device.ReadDeviceStatus();
			}
			m_newDevices.Clear();
		}

		// force send IO updates in intervals
		m_nextIO_Update -= Time.deltaTime;
		bool doUpdate = m_nextIO_Update <= 0;
		if (doUpdate)
		{
			foreach (var kv in m_remoteDevices)
			{
				kv.Value.SendUpdate();
			}
			m_nextIO_Update = MinimumUpdateInterval;
		}
	}


	public string GetDeviceName()
	{
		return $"XBee Coordinator";
	}


	public void GetDeviceInformation(StringBuilder sb)
	{
		sb.Append("COM Port: ").Append(COM_Port).AppendLine();
		sb.Append("Baudrate: ").Append(Baudrate).AppendLine();
		sb.Append("Name    : ").Append(m_coordinator.NodeID).AppendLine();
	}


	public void GetDevices(List<IDevice> devices)
	{
		if ((m_coordinator != null) && (m_coordinator.IsInitialized))
		{
			devices.Add(this);
			devices.AddRange(m_remoteDevices.Values);
		}
	}


	protected class XBee : IDevice
	{
		public XBee(XBeeManager manager, RemoteXBeeDevice device)
		{
			m_manager = manager;
			m_device  = device;
			m_oscVariables = new Dictionary<string, OSC_BoolVariable>();
		}


		public void ReadDeviceStatus()
		{
			m_manager.StartCoroutine(CR_ReadDeviceStatus());
		}


		protected IEnumerator CR_ReadDeviceStatus()
		{
			yield return new WaitForSeconds(0.1f);

			if ((m_device.NodeID == null) || (m_device.NodeID.Length == 0))
			{
				// Debug.Log($"Reading info of {DeviceInfo(m_device)}");
				m_device.ReadDeviceInfo();
			}

			//Debug.Log($"Reading IO state of {DeviceInfo(m_device)}");
			IOSample ioSample = m_device.ReadIOSample();
			ProcessIOSample(ioSample);
			//StringBuilder sb = new StringBuilder();
			//foreach (var kv in ioSample.DigitalValues)
			//{
			//	sb.Append($"\n- {kv.Key}={kv.Value}");
			//}
			//Debug.Log(sb);
		}


		public void ProcessIOSample(IOSample sample)
		{
			if ((m_device.NodeID == null) || (m_device.NodeID.Length == 0)) return;

			string prefix = m_device.NodeID.ToLower().Replace(" ", "_");
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
				varName = m_manager.OSC_Prefix + prefix + "/" + varName.Replace("dio", "input"); ;

				// search or create OSC variable
				if (!m_oscVariables.TryGetValue(varName, out OSC_BoolVariable oscVar))
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


		public void SendUpdate()
		{
			foreach (var kv in m_oscVariables)
			{
				kv.Value.SendUpdate();
			}
		}


		public string GetDeviceName()
		{
			return m_device.NodeID;
		}


		public void GetDeviceInformation(StringBuilder sb)
		{
			foreach (var kv in m_oscVariables)
			{
				sb.Append(kv.Key).Append(": ").Append(kv.Value.Value ? "■" : "□").AppendLine();
			}
		}

		protected XBeeManager                          m_manager;
		protected RemoteXBeeDevice                     m_device;
		protected Dictionary<string, OSC_BoolVariable> m_oscVariables;
	}


	protected static string DeviceInfo(AbstractXBeeDevice device)
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


	protected WinSerialPort m_serialPort;
	protected XBeeDevice    m_coordinator;
	protected XBeeNetwork   m_network;

	protected double        m_nextIO_Update;

	protected Dictionary<XBee64BitAddress, XBee> m_remoteDevices;
	protected List<XBee>                         m_newDevices;
}
