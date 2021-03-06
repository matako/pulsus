﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Hosting;
using Pulsus.Internal;
using Pulsus.Targets;

namespace Pulsus.Configuration
{
    public class PulsusConfiguration : IDisposable
    {
        private static PulsusConfiguration _defaultConfiguration;

        public PulsusConfiguration()
        {
            DefaultEventLevel = LoggingEventLevel.Information;
            Enabled = true;
            Debug = false;
            DebugVerbose = false;
            Targets = new Dictionary<string, Target>(StringComparer.OrdinalIgnoreCase);
            ExceptionsToIgnore = new Dictionary<string, Predicate<Exception>>(StringComparer.OrdinalIgnoreCase);

            LogKey = AppDomain.CurrentDomain.FriendlyName;
            if (HostingEnvironment.IsHosted)
                LogKey = HostingEnvironment.SiteName;
        }

        /// <summary>
        /// Gets or sets whether Pulsus is enabled. If set to false no events will be pushed to any target. The default value is true.
        /// </summary>
        public virtual bool Enabled { get; set; }

        /// <summary>
        /// Gets or sets whether Pulsus is in debug mode. The default valus is false. This is useful for finding errors in pulsus or targets configuration.
        /// </summary>
        public virtual bool Debug
        {
            get { return PulsusDebugger.Debug; }
            set { PulsusDebugger.Debug = value; }
        }

        /// <summary>
        /// Gets or sets the path for the debugging file
        /// </summary>
        public virtual string DebugFile
        {
            get { return PulsusDebugger.DebugFile; }
            set { PulsusDebugger.DebugFile = value; }
        }

        /// <summary>
        /// Gets or sets whether the debug file must include detailed information. If set to true detailed information will be logged to the debug file, if not, only errors will be logged. The default value is false.
        /// </summary>
        public virtual bool DebugVerbose
        {
            get { return PulsusDebugger.DebugVerbose; }
            set { PulsusDebugger.DebugVerbose = value; }
        }

        /// <summary>
        /// Gets or sets the default LogKey for all the events. The default value is the name of the application or IIS application.
        /// </summary>
        public virtual string LogKey { get; set; }
        
        /// <summary>
        /// Gets or sets the tags to be added to all the events.
        /// </summary>
        public virtual string Tags { get; set; }

        /// <summary>
        /// Gets or sets whether Pulsus must throw exceptions when an intenal error happens. The default value is false.
        /// </summary>
        public virtual bool ThrowExceptions { get; set; }

        /// <summary>
        /// Gets or sets whether the HttpContext information must be included for all the events when available. The default value is false.
        /// </summary>
        public virtual bool IncludeHttpContext { get; set; }

        /// <summary>
        /// Gets or sets whether the Stack Trace must be included for all the events. The default value is false.
        /// </summary>
        public virtual bool IncludeStackTrace { get; set; }

        /// <summary>
        /// Gets or sets whether 404 status code HttpExceptions must be ignored. The default value is false.
        /// </summary>
        public virtual bool IgnoreNotFound { get; set; }

        /// <summary>
        /// Gets or sets whether all the targets must be wrapped with an AsyncWrapperTarget. The default value is false.
        /// </summary>
        public virtual bool Async { get; set; }

        /// <summary>
        /// Gets or sets the default event level. The default value is LoggingEventLevel.None.
        /// </summary>
        public virtual LoggingEventLevel DefaultEventLevel { get; set; }

        /// <summary>
        /// Gets or sets the keys (separated by comma) that will be hidden by default in visualization. This is useful to hide sensitive information in 
        /// the EmailTarget or other event details visualization tool.
        /// </summary>
        public virtual string HideKeys { get; set; }

        /// <summary>
        /// Gets or sets whether passwords must be included with the HttpContext information. The default value is false and entries from both
        /// Request.Form and Request.QueryString with a key containing the string 'password' its value will be replaced with stars (*).
        /// </summary>
        public virtual bool IncludeHttpContextPasswords
        {
            get { return HttpContextInformation.IncludePasswords; }
            set { HttpContextInformation.IncludePasswords = value; }
        }

        /// <summary>
        /// Gets or sets whether session information must be included with the HttpContext information. The default value is false.
        /// </summary>
        public virtual bool IncludeHttpContextSession
        {
            get { return HttpContextInformation.IncludeSession; }
            set { HttpContextInformation.IncludeSession = value; }
        }

        /// <summary>
        /// Returns a dictionary of exception evaluators which will evaluate if the exception must be ignored or not. 
        /// </summary>
        public virtual IDictionary<string, Predicate<Exception>> ExceptionsToIgnore { get; private set; }

        /// <summary>
        /// Returns a dictionary of the destination targets where the logging events may be pushed. A logging event may not necessary be pushed to all
        /// the targets in the dictionary because a Target can have filter and ignore conditions. 
        /// </summary>
        public virtual IDictionary<string, Target> Targets { get; protected set; }

        /// <summary>
        /// Adds a target to the target collection. If the Async property in the target instance is set to true or if the Async property in this class is set to
        /// true, the target instance will be wrapped in an AsyncWrapperTarget instance.
        /// </summary>
        /// <param name="name">The name of the target</param>
        /// <param name="target">The target instance</param>
        public virtual void AddTarget(string name, Target target)
        {
            if ((Async && !target.Async.HasValue) || (target.Async.HasValue && target.Async.Value))
                target = WrapWithAsyncWrapperTarget(target);

            Targets.Add(name, target);
        }

        protected virtual void Initialize()
        {
            // default targets
            if (!Targets.Any())
            {
                var databaseTarget = new DatabaseTarget();
                TypeHelpers.LoadDefaultValues(databaseTarget);
                var wrapperTarget = new WrapperTarget(databaseTarget);
                TypeHelpers.LoadDefaultValues(wrapperTarget);
                AddTarget("database", wrapperTarget);
            }

            if (IgnoreNotFound && !ExceptionsToIgnore.ContainsKey("notfound"))
                ExceptionsToIgnore.Add("notfound", IsNotFoundException);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
        }

        protected virtual Target WrapWithAsyncWrapperTarget(Target target)
        {
            var asyncTargetWrapper = new AsyncWrapperTarget(target);
            PulsusDebugger.Write("Wrapped target '{0}' with AsyncWrapperTarget", asyncTargetWrapper.Name);
            return asyncTargetWrapper;
        }

        public static PulsusConfiguration Default
        {
            get
            {
                if (_defaultConfiguration == null)
                    _defaultConfiguration = Load(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Pulsus.config"));

                return _defaultConfiguration;
            }
        }

        public static PulsusConfiguration Load(string fileName)
        {
            return new PulsusXmlConfiguration(fileName);
        }

        private static bool IsNotFoundException(Exception ex)
        {
            var httpException = ex as HttpException;
            return httpException != null && httpException.GetHttpCode() == 404;
        }
    }
}
