using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DisciplesOfTheBread.Scheduler;

public class HeavyComputation : MonoBehaviour
{
	public int m = 20;
	public int n = 1000;

	void OnEnable()
	{
		SchedulerController.Instance.Register(this);
	}

	void OnDisable()
	{
		SchedulerController.Instance.Unregister(this);
	}


	[Scheduled(ScheduledFrequency.Realtime, Multithreaded=true)]
	public void ScheduledUpdate() 
	{
		FindPrimeNumber(m); 
	}


	[Scheduled(ScheduledFrequency.Normal, Multithreaded=false)]
	public void ScheduledUpdate2() 
	{
		FindPrimeNumber(n); 
	}
		
	public long FindPrimeNumber(int n)
	{
		int count=0;
		long a = 2;
		while(count<n)
		{
			long b = 2;
			int prime = 1;// to check if found a prime
			while(b * b <= a)
			{
				if(a % b == 0)
				{
					prime = 0;
					break;
				}
				b++;
			}
			if(prime > 0)
			{
				count++;
			}
			a++;
		}
		return (--a);
	}



}
