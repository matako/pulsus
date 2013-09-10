﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Web;
using System.Web.Hosting;
using System.Xml.Linq;
using Pulsus.Internal;
using Pulsus.Targets;

namespace Pulsus.Configuration
{
	public class PulsusConfiguration
	{
		private static IDictionary<string, Type> KnownTargetTypes = GetKnownTargetTypes(); 

        public PulsusConfiguration()
        {
			DefaultEventLevel = LoggingEventLevel.Information;
			Enabled = true;
			Debug = false;
			DebugVerbose = false;
			LogKey = "Default";
			Targets = new Dictionary<string, Target>(StringComparer.OrdinalIgnoreCase);
            ExceptionsToIgnore = new Dictionary<string, Predicate<Exception>>(StringComparer.OrdinalIgnoreCase);
        }

		public static PulsusConfiguration Default
		{
			get
			{
				return GetConfiguration(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Pulsus.config"));
			}
		}

		public static PulsusConfiguration Load(string fileName)
		{
			return GetConfiguration(fileName);
		}

        /// <summary>
        /// Gets or sets whether Pulsus is enabled. If set to false no events will be pushed to any target. The default value is true.
        /// </summary>
		public bool Enabled { get; set; }

        /// <summary>
        /// Gets or sets whether Pulsus is in debug mode. The default valus is false. This is useful for finding errors in pulsus or targets configuration.
        /// </summary>
		public bool Debug { get; set; }
		
        /// <summary>
        /// Gets or sets the path 
        /// </summary>
        public string DebugFile { get; set; }

        /// <summary>
        /// Gets or sets whether the debug information should be sent to the Windows Event Log. The default value is false.
        /// </summary>
	    public bool DebugToEventLog { get; set; }

	    /// <summary>
        /// Gets or sets whether the debug file must include detailed information. If set to true detailed information will be logged to the debug file, if not, only errors will be logged. The default value is false.
        /// </summary>
		public bool DebugVerbose { get; set; }

        /// <summary>
        /// Gets or sets the default LogKey for all the events. The default value is the name of the application or IIS application.
        /// </summary>
		public string LogKey { get; set; }
		
        /// <summary>
        /// Gets or sets the tags to be added to all the events.
        /// </summary>
        public string Tags { get; set; }

        /// <summary>
        /// Gets or sets whether Pulsus must throw exceptions when an intenal error happens. The default value is false.
        /// </summary>
		public bool ThrowExceptions { get; set; }

        /// <summary>
        /// Gets or sets whether the HttpContext information must be included for all the events when available. The default value is false.
        /// </summary>
		public bool IncludeHttpContext { get; set; }

        /// <summary>
        /// Gets or sets whether the Stack Trace must be included for all the events. The default value is false.
        /// </summary>
        public bool IncludeStackTrace { get; set; }

        /// <summary>
        /// Gets or sets whether 404 status code HttpExceptions must be ignored. The default value is false.
        /// </summary>
        public bool IgnoreNotFound { get; set; }

        /// <summary>
        /// Gets or sets whether all the targets must be wrapped with an AsyncWrapperTarget. The default value is false.
        /// </summary>
	    public bool Async { get; set; }

	    public IDictionary<string, Predicate<Exception>> ExceptionsToIgnore { get; private set; }
        public LoggingEventLevel DefaultEventLevel { get; set; }
		public IDictionary<string, Target> Targets { get; private set; } 

		private static PulsusConfiguration GetConfiguration(string fileName)
		{
			var configuration = new PulsusConfiguration();

            if (HostingEnvironment.IsHosted)
                configuration.LogKey = HostingEnvironment.SiteName;

		    if (File.Exists(fileName))
			{
				var xDocument = XDocument.Load(fileName);
				var root = xDocument.Root;
				LoadAttributes(configuration, root);
				configuration.Targets = GetTargets(root);
			}

			if (!configuration.Targets.Any())
			{
				var databaseTarget = new DatabaseTarget();
				LoadDefaultValues(databaseTarget);
				var wrapperTarget = new WrapperTarget(databaseTarget);
				LoadDefaultValues(wrapperTarget);

				configuration.Targets.Add("database", wrapperTarget);
			}

            if (configuration.IgnoreNotFound)
                configuration.ExceptionsToIgnore.Add("notfound", IsNotFoundException);

			SetupDebugging(configuration);

		    return configuration;
		}

		private static void SetupDebugging(PulsusConfiguration configuration)
		{
			if (configuration.Debug)
			{
				if (HostingEnvironment.IsHosted)
				{
					if (string.IsNullOrEmpty(configuration.DebugFile))
						configuration.DebugFile = "~/App_Data/pulsus_log.txt";

					configuration.DebugFile = HostingEnvironment.MapPath(configuration.DebugFile);
				}
				else
				{
					if (string.IsNullOrEmpty(configuration.DebugFile))
						configuration.DebugFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "pulsus_log.txt");
				}
			}
		}

		private static IDictionary<string, Target> GetTargets(XElement xElement)
		{
			var targets = new Dictionary<string, Target>(StringComparer.OrdinalIgnoreCase);

			var targetsElement = xElement.Element("targets");
			if (targetsElement == null)
				return targets;

			foreach (var targetElement in targetsElement.Elements("target"))
			{
				var target = GetTarget(targetElement);
				if (target != null)
					targets.Add(target.Name, target);
			}

			return targets;
		}

		private static Target GetTarget(XElement targetElement)
		{
			if (targetElement == null)
				return null;

			var name = GetAttributeValue(targetElement, "name");
			var typeName = GetAttributeValue(targetElement, "type");

			if (typeName == null)
				typeName = name.EndsWith("target", StringComparison.OrdinalIgnoreCase) ? name : name + "Target";

			var targetType = FindTargetType(typeName);
			if (targetType == null)
				return null;

			if (typeof(WrapperTarget).IsAssignableFrom(targetType))
			{
				var wrappedTargetElement = targetElement.Element("target");
				if (wrappedTargetElement == null)
					throw new Exception("No child target element for wrapper target");

				var wrappedTarget = GetTarget(wrappedTargetElement);
				return CreateTarget(targetType, targetElement, wrappedTarget);
			}

			return CreateTarget(targetType, targetElement);
		}

		private static Target CreateTarget(Type type, XElement targetAttributes = null, Target wrappedTarget = null)
		{
			var target = wrappedTarget == null ? (Target)Activator.CreateInstance(type) : (Target)Activator.CreateInstance(type, wrappedTarget);
			LoadDefaultValues(target);
			LoadAttributes(target, targetAttributes);
			return target;
		}

		private static void LoadDefaultValues(object instance)
		{
			var properties = instance.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
			foreach (var property in properties)
			{
				var defaultValue = (DefaultValueAttribute)property.GetCustomAttributes(typeof(DefaultValueAttribute), true).FirstOrDefault();			
				if (defaultValue != null)
					property.SetValue(instance, defaultValue.Value, null);
			}
		}

		private static void LoadAttributes(object instance, XElement targetElement)
		{
			foreach (var xAttribute in targetElement.Attributes())
			{
				var property = instance.GetType().GetProperty(xAttribute.Name.LocalName, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
				if (property == null || !property.CanWrite)
					continue;

				try
				{
                    if (property.PropertyType == typeof(LoggingEventLevel))
                    {
                        try
                        {
                            var enumValue = Enum.Parse(typeof (LoggingEventLevel), xAttribute.Value);
                            property.SetValue(instance, enumValue, null);
                        }
                        catch (Exception)
                        {
                        }
                        
                        continue;
                    }

				    var value = Convert.ChangeType(xAttribute.Value, property.PropertyType);
					property.SetValue(instance, value, null);
				}
				catch (Exception)
				{
				}
			}
		}

		private static string GetAttributeValue(XElement xElement, string name)
		{
			if (xElement == null)
				return null;

			var attribute = xElement.Attribute(name);

			if (attribute == null)
				return null;

			return attribute.Value;
		}

		private static Type FindTargetType(string name)
		{
			Type targetType;
			if (KnownTargetTypes.TryGetValue(name, out targetType))
				return targetType;

			return null;
		}

		private static IDictionary<string, Type> GetKnownTargetTypes()
		{
			var targetType = typeof(Target);
			var dictionary = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase);

			var targetTypes = TypeHelpers.GetFilteredTypesFromAssemblies(targetType.IsAssignableFrom);
			foreach (var type in targetTypes)
			{
				dictionary.Add(type.Name, type);
			}
			
			return dictionary;
		}

        private static bool IsNotFoundException(Exception ex)
        {
            var httpException = ex as HttpException;
            return httpException != null && httpException.GetHttpCode() == 404;
        }
	}
}
