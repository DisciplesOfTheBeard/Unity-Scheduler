using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public enum ScheduledFrequency
{
	Rare = 5, // up to a couple per second
	Infrequent = 50, // up to half of the time
	Normal = 100, // up to every frame
	Realtime = 101, // every frame
	Custom = -1, // Can be defined with the property Frequency
}
	
[AttributeUsage(AttributeTargets.Method, AllowMultiple=true, Inherited=false)]
public class Scheduled : Attribute
{
	public int Frequency { get; set; }

	public bool Multithreaded { get; set; }

	private ScheduledFrequency ScheduleFrequency { get; set; }

	public Scheduled(ScheduledFrequency frequency = ScheduledFrequency.Normal)
	{
		ScheduleFrequency = frequency;
		if (frequency != ScheduledFrequency.Custom) 
		{
			Frequency = (int)frequency;
		}
	}

}