using System.Collections.Generic;
using System.Text;
using SentienceLab.OSC;
using UnityEngine;

public class OSCDeviceInformationProvider : MonoBehaviour, IDeviceManager, IDevice
{
	public void Start()
	{
		m_oscContainers = GetComponents<IOSCVariableContainer>();
	}

	
	public void Update()
	{
		// empty
	}


	public void GetDevices(List<IDevice> devices)
	{
		devices.Add(this);
	}


	public string GetDeviceName()
	{
		return name;
	}


	public void GetDeviceInformation(StringBuilder sb, string prefix)
	{
		foreach (var c in m_oscContainers)
		{
			foreach (var v in c.GetOSC_Variables())
			{
				sb.Append(prefix).Append(v.Name).Append(": ");
				if      (v is OSC_BoolVariable     vb) { sb.Append(vb.Value ? "■" : "□"); }
				else if (v is OSC_IntVariable      vi) { sb.Append(vi.Value); }
				else if (v is OSC_FloatVariable    vf) { sb.Append(vf.Value); }
				else if (v is OSC_StringVariable   vs) { sb.Append(vs.Value); }
				else if (v is OSC_6DofPoseVariable v6) { sb.Append(string.Format("X:{0:F3}/Y:{1:F3}/Z:{2:F3}", v6.Position.x, v6.Position.y, v6.Position.z)); }
				sb.AppendLine();
			}
		}
	}


	protected IOSCVariableContainer[] m_oscContainers;
}
