using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Diagnostics;
using System;
using System.Reflection;
using System.Threading;
using System.Collections.Concurrent;

namespace DisciplesOfTheBeard.Scheduler
{
	public class SchedulerController : MonoBehaviour 
	{
		private Type[] schedulersTypes = new Type[] 
		{ 
			typeof(BaseBehaviourScheduler), 
			typeof(SpreadFIFOScheduler), 
			typeof(MultilevelQueueScheduler),
		};

		public enum Scheduler
		{
			BaseFIFO = 0,
			SpreadFIFO = 1,
			MultilevelQueue = 2,
		}

		public static SchedulerController Instance = null;

		private IScheduler scheduler;

		[SerializeField]
		private Scheduler schedulerAlgorithm = Scheduler.BaseFIFO;

		public int desiredFrameRate = 60;
		private long desiredExecutionTime;

		private bool alive = true;

		private Thread[] threads = null;

		public ConcurrentQueue<Action> multithreadQueue = new ConcurrentQueue<Action>();

		public Scheduler SchedulerAlgorithm
		{
			get { return schedulerAlgorithm; } 
			set 
			{ 
				schedulerAlgorithm = value;

				OnValidate ();
			}
		}

		public bool Register(MonoBehaviour job)
		{
			Type type = job.GetType ();

	        bool registered = false;

			foreach (MethodInfo m in type.GetMethods()) 
			{
				foreach (Attribute a in m.GetCustomAttributes(false)) 
				{
					if (a is Scheduled) 
					{
	                    registered = true;
	                    scheduler.QueueManager.Enqueue((Scheduled)a, () => m.Invoke(job, null));
					}
				}
			}

	        return registered;
		}

	    public bool Unregister(MonoBehaviour job)
		{
	        Type type = job.GetType ();

	        bool unregistered = false;

	        foreach (MethodInfo m in type.GetMethods()) 
	        {
	            foreach (Attribute a in m.GetCustomAttributes(false)) 
	            {
	                if (a is Scheduled) 
	                {
	                    unregistered = true;
	                    scheduler.QueueManager.Dequeue((Scheduled)a);
	                }
	            }
	        }

	        return unregistered;
		}

		void Awake()
		{
			Instance = this;

			OnValidate();

			PrepareThreadPool ();

			StartCoroutine (Workload());
		}

		void PrepareThreadPool()
		{
			int nbCpuCores = SystemInfo.processorCount - 1;
			threads = new Thread[nbCpuCores];

			for (int i = 0; i < nbCpuCores; i++) 
			{
				int r = UnityEngine.Random.Range (0, 50);
				threads [i] = new Thread (() => { 
					Thread.Sleep (r);
					Action a;
					while (alive) {
						Thread.Sleep (10);
						if (multithreadQueue.IsEmpty == false) {
							if (multithreadQueue.TryDequeue (out a))
								a ();
						}
					}	
				});
				threads[i].Start();
			}
		}

		void OnValidate()
		{
			// 1.2f to consider letting at minimum 20% of time frame to the rest of the computation
			desiredExecutionTime = (long)(1f / (desiredFrameRate * 1.2f)  * 1000); // miliseconds 
		
			scheduler = (IScheduler) Activator.CreateInstance(schedulersTypes [(int)schedulerAlgorithm]);
		}
			
		// TODO: Use more advanced scheduling algorithms
		IEnumerator Workload() 
		{
			while (alive) 
			{
				IEnumerator iter = scheduler.Schedule(desiredExecutionTime, desiredFrameRate);

				while (iter.MoveNext ())
					yield return iter.Current;

				yield return null;
			}
		}

		void OnDestroy()
		{
			alive = false;
		}
	}
}