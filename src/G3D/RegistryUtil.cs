/**
  @file RegistryUtil.h

  @created 2006-04-06
  @edited  2006-04-06

  Copyright 2000-2006, Morgan McGuire.
  All rights reserved.
*/

using System;
using global::System.IO;
using global::System.Security;
using Microsoft.Win32;

namespace G3D
{
    /** 
        Provides generalized Windows registry querying.

        All key names are one string in the format:
            "[base key]\[sub-keys]\value"

        [base key] can be any of the following:
            HKEY_CLASSES_ROOT
            HKEY_CURRENT_CONFIG
            HKEY_CURRENT_USER
            HKEY_LOCAL_MACHINE
            HKEY_PERFORMANCE_DATA
            HKEY_PERFORMANCE_NLSTEXT
            HKEY_PERFORMANCE_TEXT
            HKEY_USERS

        keyExists() should be used to validate a key before reading or writing
        to ensure that a debug assert or false return is for a different error.
    */
    public static class RegistryUtil
    {
        /** returns true if the key exists */
        public static bool keyExists(string key)
        {
            requireSupportedPlatform();

            RegistryPath path;
            if (!tryParseKey(key, false, out path))
            {
                return false;
            }

            try
            {
                using (RegistryKey baseKey = RegistryKey.OpenBaseKey(path.hive, RegistryView.Default))
                using (RegistryKey openKey = openSubKey(baseKey, path.subKey))
                {
                    return openKey != null;
                }
            }
            catch (UnauthorizedAccessException)
            {
                return false;
            }
            catch (IOException)
            {
                return false;
            }
            catch (SecurityException)
            {
                return false;
            }
            catch (ArgumentException)
            {
                return false;
            }
        }

        /** returns false if the key could not be read for any reason. */
        public static bool readInt32(string key, ref int valueData)
        {
            requireSupportedPlatform();

            RegistryPath path;
            if (!tryParseKey(key, true, out path))
            {
                return false;
            }

            try
            {
                using (RegistryKey baseKey = RegistryKey.OpenBaseKey(path.hive, RegistryView.Default))
                using (RegistryKey openKey = openSubKey(baseKey, path.subKey))
                {
                    if (openKey == null)
                    {
                        return false;
                    }

                    object value = openKey.GetValue(
                        path.valueName.Length == 0 ? null : path.valueName,
                        null,
                        RegistryValueOptions.DoNotExpandEnvironmentNames);
                    if (!(value is int))
                    {
                        return false;
                    }

                    valueData = (int)value;
                    return true;
                }
            }
            catch (UnauthorizedAccessException)
            {
                return false;
            }
            catch (IOException)
            {
                return false;
            }
            catch (SecurityException)
            {
                return false;
            }
            catch (ArgumentException)
            {
                return false;
            }
        }

        /** 
          Reads an arbitrary amount of data from a binary registry key.
          returns false if the key could not be read for any reason.
        
          @beta
          @param valueData pointer to the output buffer of sufficient size. Pass NULL as valueData in order to have available data size returned in dataSize.
          @param dataSize size of the output buffer.  When NULL is passed for valueData, contains the size of available data on successful return.
        */
        public static bool readBytes(string key, byte[] valueData, ref uint dataSize)
        {
            requireSupportedPlatform();

            RegistryPath path;
            if (!tryParseKey(key, true, out path))
            {
                return false;
            }
            if (valueData != null)
            {
                if (dataSize > int.MaxValue)
                {
                    throw new ArgumentOutOfRangeException("dataSize");
                }
                if (dataSize > valueData.Length)
                {
                    throw new ArgumentException("The output buffer is shorter than dataSize.", "valueData");
                }
            }

            try
            {
                using (RegistryKey baseKey = RegistryKey.OpenBaseKey(path.hive, RegistryView.Default))
                using (RegistryKey openKey = openSubKey(baseKey, path.subKey))
                {
                    if (openKey == null)
                    {
                        return false;
                    }

                    object value = openKey.GetValue(
                        path.valueName.Length == 0 ? null : path.valueName,
                        null,
                        RegistryValueOptions.DoNotExpandEnvironmentNames);
                    byte[] bytes = value as byte[];
                    if (bytes == null)
                    {
                        return false;
                    }

                    if (valueData == null)
                    {
                        dataSize = checked((uint)bytes.Length);
                        return true;
                    }

                    if (bytes.Length > (int)dataSize)
                    {
                        dataSize = checked((uint)bytes.Length);
                        return false;
                    }

                    Array.Copy(bytes, valueData, bytes.Length);
                    dataSize = checked((uint)bytes.Length);
                    return true;
                }
            }
            catch (UnauthorizedAccessException)
            {
                return false;
            }
            catch (IOException)
            {
                return false;
            }
            catch (SecurityException)
            {
                return false;
            }
            catch (ArgumentException)
            {
                return false;
            }
        }

        /** returns false if the key could not be read for any reason. */
        public static bool readString(string key, ref string valueData)
        {
            requireSupportedPlatform();

            RegistryPath path;
            if (!tryParseKey(key, true, out path))
            {
                return false;
            }

            try
            {
                using (RegistryKey baseKey = RegistryKey.OpenBaseKey(path.hive, RegistryView.Default))
                using (RegistryKey openKey = openSubKey(baseKey, path.subKey))
                {
                    if (openKey == null)
                    {
                        return false;
                    }

                    object value = openKey.GetValue(
                        path.valueName.Length == 0 ? null : path.valueName,
                        null,
                        RegistryValueOptions.DoNotExpandEnvironmentNames);
                    string text = value as string;
                    if (text == null)
                    {
                        return false;
                    }

                    valueData = text;
                    return true;
                }
            }
            catch (UnauthorizedAccessException)
            {
                return false;
            }
            catch (IOException)
            {
                return false;
            }
            catch (SecurityException)
            {
                return false;
            }
            catch (ArgumentException)
            {
                return false;
            }
        }

        /** returns false if the key could not be written for any reason. */
        public static bool writeInt32(string key, int valueData)
        {
            requireSupportedPlatform();

            RegistryPath path;
            if (!tryParseKey(key, true, out path))
            {
                return false;
            }

            try
            {
                using (RegistryKey baseKey = RegistryKey.OpenBaseKey(path.hive, RegistryView.Default))
                using (RegistryKey openKey = openSubKey(baseKey, path.subKey))
                {
                    if (openKey == null)
                    {
                        return false;
                    }

                    openKey.SetValue(
                        path.valueName.Length == 0 ? null : path.valueName,
                        valueData,
                        RegistryValueKind.DWord);
                    return true;
                }
            }
            catch (UnauthorizedAccessException)
            {
                return false;
            }
            catch (IOException)
            {
                return false;
            }
            catch (SecurityException)
            {
                return false;
            }
            catch (ArgumentException)
            {
                return false;
            }
        }

        /** 
          Writes an arbitrary amount of data to a binary registry key.
          returns false if the key could not be written for any reason.
        
          @param valueData pointer to the input buffer
          @param dataSize size of the input buffer that should be written
        */
        public static bool writeBytes(string key, byte[] valueData, uint dataSize)
        {
            requireSupportedPlatform();

            if (valueData == null)
            {
                throw new ArgumentNullException("valueData");
            }
            if (dataSize > int.MaxValue)
            {
                throw new ArgumentOutOfRangeException("dataSize");
            }
            if (dataSize > valueData.Length)
            {
                throw new ArgumentException("dataSize is larger than the input buffer.", "dataSize");
            }

            RegistryPath path;
            if (!tryParseKey(key, true, out path))
            {
                return false;
            }

            try
            {
                using (RegistryKey baseKey = RegistryKey.OpenBaseKey(path.hive, RegistryView.Default))
                using (RegistryKey openKey = openSubKey(baseKey, path.subKey))
                {
                    if (openKey == null)
                    {
                        return false;
                    }

                    byte[] bytes = new byte[(int)dataSize];
                    Array.Copy(valueData, bytes, bytes.Length);
                    openKey.SetValue(
                        path.valueName.Length == 0 ? null : path.valueName,
                        bytes,
                        RegistryValueKind.Binary);
                    return true;
                }
            }
            catch (UnauthorizedAccessException)
            {
                return false;
            }
            catch (IOException)
            {
                return false;
            }
            catch (SecurityException)
            {
                return false;
            }
            catch (ArgumentException)
            {
                return false;
            }
        }

        /** returns false if the key could not be written for any reason. */
        public static bool writeString(string key, string valueData)
        {
            requireSupportedPlatform();

            if (valueData == null)
            {
                throw new ArgumentNullException("valueData");
            }

            RegistryPath path;
            if (!tryParseKey(key, true, out path))
            {
                return false;
            }

            try
            {
                using (RegistryKey baseKey = RegistryKey.OpenBaseKey(path.hive, RegistryView.Default))
                using (RegistryKey openKey = openSubKey(baseKey, path.subKey))
                {
                    if (openKey == null)
                    {
                        return false;
                    }

                    openKey.SetValue(
                        path.valueName.Length == 0 ? null : path.valueName,
                        valueData,
                        RegistryValueKind.String);
                    return true;
                }
            }
            catch (UnauthorizedAccessException)
            {
                return false;
            }
            catch (IOException)
            {
                return false;
            }
            catch (SecurityException)
            {
                return false;
            }
            catch (ArgumentException)
            {
                return false;
            }
        }

        private sealed class RegistryPath
        {
            public RegistryHive hive;
            public string subKey;
            public string valueName;
        }

        private static RegistryKey openSubKey(RegistryKey baseKey, string subKey)
        {
            if (subKey.Length == 0)
            {
                return baseKey;
            }
            return baseKey.OpenSubKey(subKey, true);
        }

        private static bool tryParseKey(string key, bool includeValue, out RegistryPath path)
        {
            path = null;
            if (key == null)
            {
                return false;
            }

            int separator = key.IndexOf('\\');
            if (separator < 0)
            {
                return false;
            }

            RegistryHive hive;
            if (!tryGetHive(key.Substring(0, separator), out hive))
            {
                return false;
            }

            string remainder = key.Substring(separator + 1);
            path = new RegistryPath();
            path.hive = hive;

            if (!includeValue)
            {
                path.subKey = remainder;
                path.valueName = "";
                return true;
            }

            int valueSeparator = remainder.LastIndexOf('\\');
            if (valueSeparator < 0)
            {
                path.subKey = "";
                path.valueName = remainder;
            }
            else
            {
                path.subKey = remainder.Substring(0, valueSeparator);
                path.valueName = remainder.Substring(valueSeparator + 1);
            }
            return true;
        }

        private static bool tryGetHive(string name, out RegistryHive hive)
        {
            if (name == "HKEY_CLASSES_ROOT")
            {
                hive = RegistryHive.ClassesRoot;
                return true;
            }
            if (name == "HKEY_CURRENT_CONFIG")
            {
                hive = RegistryHive.CurrentConfig;
                return true;
            }
            if (name == "HKEY_CURRENT_USER")
            {
                hive = RegistryHive.CurrentUser;
                return true;
            }
            if (name == "HKEY_LOCAL_MACHINE")
            {
                hive = RegistryHive.LocalMachine;
                return true;
            }
            if (name == "HKEY_PERFORMANCE_DATA")
            {
                hive = RegistryHive.PerformanceData;
                return true;
            }
            if (name == "HKEY_PERFORMANCE_NLSTEXT")
            {
                throw new NotSupportedException(
                    "HKEY_PERFORMANCE_NLSTEXT is not exposed by Microsoft.Win32.Registry.");
            }
            if (name == "HKEY_PERFORMANCE_TEXT")
            {
                throw new NotSupportedException(
                    "HKEY_PERFORMANCE_TEXT is not exposed by Microsoft.Win32.Registry on .NET Framework 4.0.");
            }
            if (name == "HKEY_USERS")
            {
                hive = RegistryHive.Users;
                return true;
            }

            hive = RegistryHive.ClassesRoot;
            return false;
        }

        private static void requireSupportedPlatform()
        {
            PlatformID platform = Environment.OSVersion.Platform;
            if (platform != PlatformID.Win32NT &&
                platform != PlatformID.Win32Windows &&
                platform != PlatformID.Win32S)
            {
                throw new PlatformNotSupportedException(
                    "RegistryUtil is supported only on Windows.");
            }
        }
    }
}
