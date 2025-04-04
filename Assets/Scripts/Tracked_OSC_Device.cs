using SentienceLab.OSC;
using UnityEngine;

[AddComponentMenu("VP/Tracked OSC Device")]
public class Tracked_OSC_Device : MonoBehaviour
{
	public string OSC_Prefix = "/tracked_device";


	public void Start()
	{
		m_pose = new OSC_6DofPoseVariable(OSC_Prefix + "/pose", OSC_6DofPoseVariable.EDataFormat.Pos_RotQuat);
	}

	
	public void Update()
	{
		m_pose.Position = transform.position;
		m_pose.Rotation = transform.rotation;
		m_pose.SendUpdate();
	}


	OSC_6DofPoseVariable m_pose;
}
