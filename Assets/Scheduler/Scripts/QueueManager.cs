using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace DisciplesOfTheBread.Scheduler
{
	public interface IQueueManager
	{
		ICollection Jobs { get;	}

		bool Enqueue(Scheduled s, Action a);
		bool Dequeue(Scheduled s);
	}

	public class BaseQueueManager : IQueueManager
	{
		internal Dictionary<Scheduled, Action> jobs = new Dictionary<Scheduled, Action>(100);

		public ICollection Jobs
		{
			get { return jobs.Values; }
		}

		public bool Enqueue (Scheduled s, Action a)
		{
			try 
			{
				jobs.Add(s, a);
				return true;
			}
			catch(ArgumentException)
			{
				return false;
			}
		}

		public bool Dequeue(Scheduled s)
		{
			try 
			{
				jobs.Remove(s);
				return true;
			}
			catch(ArgumentException)
			{
				return false;
			}
		}
	}

	public class MultilevelQueueManager : IQueueManager
	{
		internal Dictionary<int, Dictionary<Scheduled, Action>> jobs = new Dictionary<int, Dictionary<Scheduled, Action>>(100);

		// TODO: Useless here
		public ICollection Jobs 
		{
			get 
			{
				return jobs.Values;
			}
		}

		public bool Enqueue(Scheduled s, Action a)
		{
			if (jobs.ContainsKey(s.Frequency) == false)
				jobs.Add(s.Frequency, new Dictionary<Scheduled,Action>(10));

			try 
			{
				jobs[s.Frequency].Add(s, a);
				return true;
			}
			catch (ArgumentException)
			{
				return false;
			}
		}

		public bool Dequeue(Scheduled s)
		{
			if (jobs.ContainsKey (s.Frequency) == false)
				return false;

			try 
			{
				jobs[s.Frequency].Remove(s);
				return true;
			}
			catch (Exception)
			{
				return false;
			}
		}
	}
}