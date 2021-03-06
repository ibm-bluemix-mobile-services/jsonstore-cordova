﻿/*
 *     Copyright 2016 IBM Corp.
 *     Licensed under the Apache License, Version 2.0 (the "License");
 *     you may not use this file except in compliance with the License.
 *     You may obtain a copy of the License at
 *     http://www.apache.org/licenses/LICENSE-2.0
 *     Unless required by applicable law or agreed to in writing, software
 *     distributed under the License is distributed on an "AS IS" BASIS,
 *     WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 *     See the License for the specific language governing permissions and
 *     limitations under the License.
 */

using System;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace JSONStoreWin8Lib.JSONStore
{
    public class JSONStoreException : System.Exception
    {
        public int errorCode { get; set; }
        public JArray data { get; set; }

        public JSONStoreException()
        {
        }

        public JSONStoreException(string message)
        {
        }

        public JSONStoreException(string message, Exception inner)
        {
        }

        public JSONStoreException(int errorCode)
        {
            this.errorCode = errorCode;
        }

        public JSONStoreException(int errorCode, JArray data)
        {
            this.errorCode = errorCode;
            this.data = data;
        }
    }
}
