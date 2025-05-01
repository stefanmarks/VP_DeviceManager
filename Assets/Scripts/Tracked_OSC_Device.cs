using SentienceLab.OSC;
using System.Collections.Generic;
using UnityEngine;

[AddComponentMenu("VP/Tracked OSC Device")]
public class Tracked_OSC_Device : MonoBehaviour, IOSCVariableContainer
{
	[Header("OSC")]
	public string OSC_Prefix            = "/tracked_device";
	public float  MinimumUpdateInterval = 1.0f;

	[Header("Device")]
	public Transform TrackedObject = null; 


	public void Start()
	{
		if (TrackedObject == null)
		{ 
			TrackedObject = transform; 
		}

		m_pose    = new OSC_6DofPoseVariable(OSC_Prefix + "/pose", OSC_6DofPoseVariable.EDataFormat.Pos_RotQuat);
		m_tracked = new OSC_BoolVariable(    OSC_Prefix + "/tracked");
		m_oscVariables = new List<OSC_Variable> { m_pose, m_tracked };

		m_nextOSCUpdate = 0;
	}

	
	public void Update()
	{
		m_pose.Position = TrackedObject.position;
		m_pose.Rotation = TrackedObject.rotation;
		m_pose.SendUpdate();

		m_nextOSCUpdate -= Time.unscaledDeltaTime;
		bool doUpdate = m_nextOSCUpdate <= 0;

		if (m_tracked.Value != TrackedObject.gameObject.activeSelf)
		{
			m_tracked.Value = TrackedObject.gameObject.activeSelf;
			doUpdate = true;
		}

		if (doUpdate)
		{
			m_tracked.SendUpdate();
			m_nextOSCUpdate = MinimumUpdateInterval;
		}
	}


	public List<OSC_Variable> GetOSC_Variables()
	{
		return m_oscVariables;
	}


	protected OSC_6DofPoseVariable m_pose;
	protected OSC_BoolVariable     m_tracked;
	protected List<OSC_Variable>   m_oscVariables;
	protected double               m_nextOSCUpdate;
}
