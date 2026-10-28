/**
  @file DXCaps.h

  @created 2006-04-06
  @edited  2006-05-06

  @maintainer Corey Taylor

  Copyright 2000-2006, Morgan McGuire.
  All rights reserved.
*/

using System;
using G3D;
using SharpDX.DXGI;

namespace G3D.Direct3D11 {

    /**
        Provides very basic DirectX detection and information support
    */
    public static class DXCaps {

        private static uint dxVersion = 0;
        private static bool versionInitialized = false;
        private static ulong totalVidSize = 0;
        private static bool videoMemoryInitialized = false;
        private static readonly object initializationLock = new object();

        /**
            Returns 0 if not installed otherwise returns the major and minor number
            in the form (major * 100) + minor.  eg. 900 is 9.0 and 901 is 9.1
        */
        public static uint version() {
            if (versionInitialized) {
                return dxVersion;
            }

            lock (initializationLock) {
                if (versionInitialized) {
                    return dxVersion;
                }

            if ( RegistryUtil.keyExists("HKEY_LOCAL_MACHINE\\Software\\Microsoft\\DirectX") ) {
                string versionString = null;
                if ( RegistryUtil.readString("HKEY_LOCAL_MACHINE\\Software\\Microsoft\\DirectX\\Version", ref versionString) ) {
                    char[] major = new char[3], minor = new char[3];
                    if ( versionString != null && versionString.Length >= 7 ) {
                        major[0] = versionString[2];
                        major[1] = versionString[3];
                        major[2] = '\0';

                        minor[0] = versionString[5];
                        minor[1] = versionString[6];
                        minor[2] = '\0';

                        dxVersion = (uint)((atoi(new string(major)) * 100) + atoi(new string(minor)));
                    }
                }
            }

            versionInitialized = true;

            return dxVersion;
            }
        }

        /**
            Returns the amount of video memory detected by Direct3D in bytes.
            This may not be the true amount of physical RAM; some graphics cards
            over or under report.
        */
        public static ulong videoMemorySize() {
            if (videoMemoryInitialized) {
                return totalVidSize;
            }

            lock (initializationLock) {
                if (videoMemoryInitialized) {
                    return totalVidSize;
                }

                try {
                    using (Factory1 factory = new Factory1()) {
                        int adapterCount = factory.GetAdapterCount1();
                        for (int adapterIndex = 0; adapterIndex < adapterCount; ++adapterIndex) {
                            Adapter1 adapter = null;
                            try {
                                adapter = factory.GetAdapter1(adapterIndex);
                            } catch (global::SharpDX.SharpDXException) {
                                break;
                            }
                            if (adapter == null) {
                                break;
                            }
                            using (adapter) {
                                ulong dedicated = (ulong)(long)adapter.Description.DedicatedVideoMemory;
                                if (dedicated > totalVidSize) {
                                    totalVidSize = dedicated;
                                }
                            }
                        }
                    }
                } catch (Exception) {
                    totalVidSize = 0;
                }

                videoMemoryInitialized = true;

                return totalVidSize;
            }
        }

        private static int atoi(string s) {
            int value = 0;
            for (int i = 0; i < s.Length; ++i) {
                char c = s[i];
                if (c < '0' || c > '9') {
                    break;
                }
                value = value * 10 + (c - '0');
            }
            return value;
        }
    }
}
