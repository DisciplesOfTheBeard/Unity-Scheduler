using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Diagnostics;

namespace DisciplesOfTheBeard.Scheduler
{
	// TODO: replace by covariant IScheduler<out T> as soon Unity supports it correctly
	 
	public interface IScheduler 
	{
		string Name { get;}
		IQueueManager QueueManager { get; }
		IEnumerator Schedule (long desiredExecutionTime, int desiredFrameRate);
	}

	public interface IScheduler<T> : IScheduler where T : IQueueManager
	{

	}

	public class BaseBehaviourScheduler : IScheduler<BaseQueueManager>
	{
		public virtual string Name
		{
			get { return "BaseFIFO"; }
		}

		public IQueueManager QueueManager 
		{
			get;
			set;
		}

		public BaseBehaviourScheduler()
		{
			QueueManager = new BaseQueueManager();
		}

		public virtual IEnumerator Schedule (long desiredExecutionTime, int desiredFrameRate)
		{
			// Base Scheduler: just run everything in the current frame
			foreach (Action a in QueueManager.Jobs)
			{
				a();
			}

			yield return null;
		}
	}

	public class SpreadFIFOScheduler : BaseBehaviourScheduler
	{
		private Stopwatch sw = new Stopwatch();

		public override string Name
		{
			get { return "SpreadFIFO"; }
		}

		public SpreadFIFOScheduler() : base() {}

		public override IEnumerator Schedule (long desiredExecutionTime, int desiredFrameRate)
		{
			// Spread FIFO Scheduler: run as much job as we can within the desired execution time then go to next frame for the rest
			sw.Reset();

			foreach (Action a in QueueManager.Jobs)
			{
				sw.Start();

				a();

				sw.Stop();

				if (sw.ElapsedMilliseconds >= desiredExecutionTime) 
				{
					sw.Reset();
					yield return null;
				}
			}
		}
	}

	public class MultilevelQueueScheduler : IScheduler<MultilevelQueueManager>
	{
		private Stopwatch sw = new Stopwatch();

		private MultilevelQueueManager queueManager;

		public virtual string Name
		{
			get { return "MultilevelQueue"; } 
		}

		public MultilevelQueueScheduler()
		{
			queueManager = new MultilevelQueueManager();
		}

		public IQueueManager QueueManager
		{
			get { return queueManager; }
		}

		private Queue<Action> PrepareQueue(ScheduledFrequency frequency)
		{
			Queue<Action> queue;
			if (queueManager.jobs.ContainsKey ((int)frequency)) 
			{
				queue = new Queue<Action> (50);

				foreach (Action a in queueManager.jobs[(int)frequency].Values)
					queue.Enqueue (a);

				return queue;
			} 

			return null;
		}

		public virtual IEnumerator Schedule(long desiredExecutionTime, int desiredFrameRate)
		{
			Action a;
			Queue<Action> normal = PrepareQueue(ScheduledFrequency.Normal);
			Queue<Action> infrequent = PrepareQueue(ScheduledFrequency.Infrequent);
			Queue<Action> rare = PrepareQueue(ScheduledFrequency.Rare);

			Func<Queue<Action>, bool> completeQueue = (q) => {
				if (q == null)
					return true;

				while (q.Count > 0)
				{
					sw.Start();

					a = q.Dequeue();
					a();

					sw.Stop ();

					if (sw.ElapsedMilliseconds >= desiredExecutionTime) 
					{
						sw.Reset();
						return false;
					}
				}
					
				return true;
			};

			sw.Reset();

			for (int frame = 1; frame <= desiredFrameRate; frame++)
			{
				sw.Start();

				// Realtime
				if (queueManager.jobs.ContainsKey ((int)ScheduledFrequency.Realtime)) 
				{
					foreach (KeyValuePair<Scheduled, Action> entry in queueManager.jobs[(int)ScheduledFrequency.Realtime]) 
					{
						if (entry.Key.Multithreaded == false)
							entry.Value ();
						else
							SchedulerController.Instance.multithreadQueue.Enqueue (entry.Value);
					}
				}

				sw.Stop ();

				if (sw.ElapsedMilliseconds >= desiredExecutionTime) 
				{
					sw.Reset();
					yield return null;
					continue;
				}

				if (completeQueue (normal) == false) 
				{
					yield return null;
					continue; // queue was not able t
				}

				if (completeQueue (infrequent) == false) 
				{
					yield return null;
					continue;
				}

				if (completeQueue (rare) == false) 
				{
					yield return null;
					continue;
				}

				yield return null;
			}
		}
	}
}