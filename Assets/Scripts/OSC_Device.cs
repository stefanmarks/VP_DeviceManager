using SentienceLab.OSC;
using UnityEngine;

public class OSC_Device : MonoBehaviour
{
	public string Prefix = "/tracked_device";
	public float  MinimumUpdateInterval = 1.0f;

	public void Start()
	{
		m_pose = new OSC_6DofPoseVariable(Prefix + "/pose", OSC_6DofPoseVariable.EDataFormat.Pos_RotQuat);
		
		m_io = GetComponent<IO_Device>();
		if (m_io != null)
		{
			m_io1 = new OSC_BoolVariable(Prefix + "/input1");
			m_io2 = new OSC_BoolVariable(Prefix + "/input2");
			m_io3 = new OSC_BoolVariable(Prefix + "/input3");
			m_io4 = new OSC_BoolVariable(Prefix + "/input4");
			m_nextIoUpdate = 0;
		}
	}

	
	public void Update()
	{
		m_pose.Position = transform.position;
		m_pose.Rotation = transform.rotation;
		m_pose.SendUpdate();

		if (m_io != null)
		{
			m_nextIoUpdate -= Time.deltaTime;
			bool doUpdate = m_nextIoUpdate <= 0;
			
			if (m_io1.Value != m_io.Input1On) { m_io1.Value = m_io.Input1On; doUpdate = true; }
			if (m_io2.Value != m_io.Input2On) { m_io2.Value = m_io.Input2On; doUpdate = true; }
			if (m_io3.Value != m_io.Input3On) { m_io3.Value = m_io.Input3On; doUpdate = true; }
			if (m_io4.Value != m_io.Input4On) { m_io4.Value = m_io.Input4On; doUpdate = true; }

			if (doUpdate)
			{
				m_io1.SendUpdate();
				m_io2.SendUpdate();
				m_io3.SendUpdate();
				m_io4.SendUpdate();
				m_nextIoUpdate = MinimumUpdateInterval;
			}
		}
	}


	OSC_6DofPoseVariable m_pose;
	IO_Device            m_io;
	OSC_BoolVariable     m_io1, m_io2, m_io3, m_io4;
	double               m_nextIoUpdate;
}
