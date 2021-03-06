﻿using System;
using System.Linq;
using Pulsus.Internal;

namespace Pulsus
{
    public class LoggingEventBuilder : LoggingEventBuilderBase<LoggingEventBuilder>
    {
        public LoggingEventBuilder()
            : this(null)
        {
        }

        public LoggingEventBuilder(LoggingEvent loggingEvent)
            : base(loggingEvent)
        {
        }
    }

    public abstract class LoggingEventBuilderBase<T> : ILoggingEventBuilder<T> where T : class, ILoggingEventBuilder<T>, new()
    {
        protected LoggingEventBuilderBase(LoggingEvent loggingEvent = null)
        {
            LoggingEvent = loggingEvent ?? new LoggingEvent();
        }

        internal static T Create()
        {
            return new T();
        }

        public LoggingEvent LoggingEvent { get; private set; }

        public virtual T LogKey(string logKey)
        {
            LoggingEvent.LogKey = logKey;
            return this as T;
        }

        public virtual T ApiKey(string apiKey)
        {
            LoggingEvent.ApiKey = apiKey;
            return this as T;
        }

        public virtual T SessionId(string psid)
        {
            LoggingEvent.Psid = psid;
            return this as T;
        }

        public virtual T PersistentSessionId(string ppid)
        {
            LoggingEvent.Ppid = ppid;
            return this as T;
        }

        public virtual T CorrelationId(string correlationId)
        {
            LoggingEvent.CorrelationId = correlationId;
            return this as T;
        }

        public virtual T Text(string text, params object[] args)
        {
            if (args == null || !args.Any())
                LoggingEvent.Text = text;
            else
                LoggingEvent.Text = string.Format(text, args);
            return this as T;
        }

        public virtual T AddData(string key, object value)
        {
            LoggingEvent.Data[key] = value;
            return this as T;
        }

        public virtual T Date(DateTime date, bool useUtc)
        {
            LoggingEvent.Date = useUtc ? date.ToUniversalTime() : date;
            return this as T;
        }

        public virtual T AddTags(params string[] tags)
        {
            foreach (var tag in TagHelpers.Clean(tags))
                LoggingEvent.Tags.Add(tag);
            return this as T;
        }

        public virtual T User(string user, params object[] args)
        {
            if (args == null || !args.Any())
                LoggingEvent.User = user;
            else
                LoggingEvent.User = string.Format(user, args);
            return this as T;
        }

        public virtual T Level(LoggingEventLevel level)
        {
            LoggingEvent.Level = level;
            return this as T;
        }

        [Obsolete("Use the overload with the LoggingEventLevel enum instead")]
        public virtual T Level(int level)
        {
            throw new InvalidOperationException("Use the overload with the LoggingEventLevel enum instead");
        }

        public virtual T Push()
        {
            AddConfiguration();

            LogManager.Push(LoggingEvent);

            return this as T;
        }

        protected void AddConfiguration()
        {
            var configurationTags = LogManager.Configuration.Tags;
            if (!string.IsNullOrEmpty(configurationTags))
            {
                foreach (var tag in TagHelpers.Clean(configurationTags))
                    LoggingEvent.Tags.Add(tag);
            }
        }
    }
}
