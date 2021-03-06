﻿using System;
using Pulsus.Internal;

namespace Pulsus
{
    public partial class LoggingEvent
    {
        public T GetData<T>(string key)
        {
            T output;

            if (TryGetData(key, out output))
                return output;

            return default(T);
        }

        public bool TryGetData<T>(string key, out T output)
        {
            if (!Data.ContainsKey(key))
            {
                output = default(T);
                return false;
            }

            var value = Data[key];

            if (value == null)
            {
                output = default(T);
                return false;
            }

            // value type matches requested type
            if (value.GetType() == typeof(T))
            {
                output = (T)value;
                return true;
            }
            
            // requested type is string
            if (typeof(T) == typeof(string))
            {
                var stringValue = value as string;
                if (stringValue != null)
                {
                    output = (T)Convert.ChangeType(stringValue, typeof(T));
                    return true;
                }
                
                output = (T)Convert.ChangeType(value.ToString(), typeof(T));
                return false;
            }

            // finally try to deserialize it to the type
            var valueToDeserialize = value.ToString();
            output = LogManager.JsonSerializer.DeserializeObject<T>(valueToDeserialize);
            return output != null;
        }
    }
}
