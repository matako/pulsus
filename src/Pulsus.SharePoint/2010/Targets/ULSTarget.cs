﻿using System;
using System.ComponentModel;
using Microsoft.SharePoint.Administration;
using Pulsus.Targets;

namespace Pulsus.SharePoint.Targets
{
    public class ULSTarget : Target
    {
        [DefaultValue(true)]
        public bool WriteTrace { get; set; }

        [DefaultValue(false)]
        public bool WriteEvent { get; set; }

        public override void Push(LoggingEvent[] loggingEvents)
        {
            foreach (var loggingEvent in loggingEvents)
            {
                var category = new SPDiagnosticsCategory(loggingEvent.LogKey, TraceSeverity.None, EventSeverity.None);

                if (WriteTrace)
                {
                    var traceSeverity = GetTraceSeverity(loggingEvent);
                    SPDiagnosticsService.Local.WriteTrace(0, category, traceSeverity, loggingEvent.Text, loggingEvent.EventId);
                }

                if (WriteEvent)
                {
                    var eventSeverity = GetEventSeverity(loggingEvent);
                    SPDiagnosticsService.Local.WriteEvent(0, category, eventSeverity, loggingEvent.Text, loggingEvent.EventId);
                }
            }
        }

        private TraceSeverity GetTraceSeverity(LoggingEvent loggingEvent)
        {
            var loggingEventLevel = (LoggingEventLevel)Enum.Parse(typeof(LoggingEventLevel), loggingEvent.Level.ToString());

            if (loggingEventLevel == LoggingEventLevel.None)
                return TraceSeverity.None;
            if (loggingEventLevel == LoggingEventLevel.Trace)
                return TraceSeverity.Verbose;
            if (loggingEventLevel == LoggingEventLevel.Debug)
                return TraceSeverity.Verbose;
            if (loggingEventLevel == LoggingEventLevel.Information)
                return TraceSeverity.Verbose;
            if (loggingEventLevel == LoggingEventLevel.Warning)
                return TraceSeverity.Monitorable;
            if (loggingEventLevel == LoggingEventLevel.Error)
                return TraceSeverity.Medium;
            if (loggingEventLevel == LoggingEventLevel.Alert)
                return TraceSeverity.High;

            return TraceSeverity.None;
        }

        private EventSeverity GetEventSeverity(LoggingEvent loggingEvent)
        {
            var loggingEventLevel = (LoggingEventLevel)Enum.Parse(typeof(LoggingEventLevel), loggingEvent.Level.ToString());

            if (loggingEventLevel == LoggingEventLevel.None)
                return EventSeverity.None;
            if (loggingEventLevel == LoggingEventLevel.Trace)
                return EventSeverity.Verbose;
            if (loggingEventLevel == LoggingEventLevel.Debug)
                return EventSeverity.Verbose;
            if (loggingEventLevel == LoggingEventLevel.Information)
                return EventSeverity.Information;
            if (loggingEventLevel == LoggingEventLevel.Warning)
                return EventSeverity.Warning;
            if (loggingEventLevel == LoggingEventLevel.Error)
                return EventSeverity.Error;
            if (loggingEventLevel == LoggingEventLevel.Alert)
                return EventSeverity.ErrorCritical;

            return EventSeverity.None;
        }
    }
}