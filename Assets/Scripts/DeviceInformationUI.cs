using SentienceLab;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TMPro;
using UnityEngine;

public class DebugInformation : MonoBehaviour
{
	[TypeConstraint(typeof(IDeviceManager))]
	public GameObject Device;

	public TMP_Text   Text;
	public float      SourceUpdateInterval = 1.0f;
	public float      InformationUpdateInterval = 0.1f;


	public void Awake()
	{
		if (Text == null)
		{
			Text = GetComponent<TMP_Text>();
		}
		
		m_managers = new List<IDeviceManager>();
		m_devices  = new List<IDevice>();
	}
	
	
	public void OnEnable()
	{
		IDeviceManager manager = (Device != null) ? Device.GetComponent<IDeviceManager>() : null;
		if (manager != null)
		{
			m_managers.Clear();
			m_managers.Add(manager);
		}
		else
		{
			StartCoroutine(GatherDeviceManagers());
		}

		StartCoroutine(UpdateInformation());
	}


	public void OnDisable()
	{
		StopCoroutine(GatherDeviceManagers());
		StopCoroutine(UpdateInformation());
	}


	public void Update()
	{
		// empty
	}


	protected IEnumerator UpdateInformation()
	{
		while (true)
		{
			yield return new WaitForSeconds(InformationUpdateInterval);

			m_devices.Clear();
			foreach (var manager in m_managers)
			{
				manager.GetDevices(m_devices);
			}
			m_devices.Sort(IDeviceComparer.INSTANCE);

			StringBuilder sb = new StringBuilder();
			foreach (var device in m_devices)
			{
				sb.Append(device.GetDeviceName()).Append(":").AppendLine();
				device.GetDeviceInformation(sb, " - ");
			}
			Text.text = sb.ToString();
		}
	}


	protected IEnumerator GatherDeviceManagers()
	{
		while (true)
		{
			yield return new WaitForSeconds(SourceUpdateInterval);
			var managers = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None).OfType<IDeviceManager>();
			m_managers.Clear();
			m_managers.AddRange(managers);
		}
	}


	protected List<IDeviceManager> m_managers;
	protected List<IDevice>        m_devices;
}
