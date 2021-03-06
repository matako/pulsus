﻿using System;
using System.Diagnostics;
using System.Threading;
using Pulsus.Internal;

namespace Pulsus.Targets
{
	public class AsyncWrapperTarget : WrapperTarget
	{
		private readonly object _locker = new object();
		private ITimer _batchTimer;
		private LoggingEventQueue _queue;
		private long _minTimerInterval;
		private long _timerInterval;

		public AsyncWrapperTarget(Target target) : base(target)
		{
			_batchTimer = new TimerWrapper(ProcessBatchQueue);
			Initialize();
		}

		internal AsyncWrapperTarget(Target target, ITimer timer) : base(target)
		{
			_batchTimer = timer;
			Initialize();
		}

		public int MaxQueueSize
		{
			get
			{
				return _queue.MaxQueueSize;
			}

			set
			{
				_queue.MaxQueueSize = value;
			}
		}

		public int BatchSize { get; set; }
		
		public long TimerInterval
		{
			get
			{
				return _timerInterval;
			}
			set
			{
				_timerInterval = value;

				if (TimerIntervalChanged != null)
					TimerIntervalChanged(value);
			}
		}

		public event Action<long> TimerIntervalChanged; 

		protected void Initialize()
		{
			_queue = new LoggingEventQueue();
			_timerInterval = 50;
			BatchSize = 100;

			_minTimerInterval = _timerInterval;
			_queue.Clear();
			StartBatchTimer();
		}

		protected virtual void StartBatchTimer()
		{
			lock (_locker)
			{
				if (_batchTimer == null)
					return;

				_batchTimer.Change(TimerInterval, Timeout.Infinite);
			}
		}

		protected virtual void StopBatchTimer()
		{
			lock (_locker)
			{
				if (_batchTimer != null)
				{
					_batchTimer.Change(Timeout.Infinite, Timeout.Infinite);
					_batchTimer = null;
				}
			}
		}

		public override void Push(LoggingEvent[] loggingEvents)
		{
			if (loggingEvents == null)
				throw new ArgumentNullException("loggingEvents");

			_queue.Enqueue(loggingEvents);
		}

		private void ProcessBatchQueue(object state)
		{
			var eventsToPush = new LoggingEvent[] {};

			try
			{
				eventsToPush = _queue.Dequeue(BatchSize);

				if (eventsToPush.Length <= 0) 
					return;

                PulsusDebugger.Write("[AsyncWrapper] Pushing {0} events to {1}.", eventsToPush.Length, WrappedTarget);

				var stopWatch = Stopwatch.StartNew();
				PushInternal(eventsToPush);
				stopWatch.Stop();

				// adjust the timer interval
				if (Math.Abs(stopWatch.ElapsedMilliseconds - TimerInterval) > 50)
					TimerInterval = Math.Max(stopWatch.ElapsedMilliseconds, _minTimerInterval);
				
			}
			catch (Exception ex)
			{
                PulsusDebugger.Error(ex, "[AsyncWrapper] Error pushing {0} events to {1}", eventsToPush.Length, WrappedTarget);

				// Add the events to the queue again to try again
				if (eventsToPush.Length > 0)
					_queue.Enqueue(eventsToPush);
			}
			finally
			{
				StartBatchTimer();
			}
		}

		public override string ToString()
		{
			return string.Format("[AsyncWrapper] {0}", WrappedTarget);
		}
	}
}
