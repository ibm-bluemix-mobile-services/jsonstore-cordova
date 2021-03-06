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

using Newtonsoft.Json.Linq;

namespace JSONStoreWin8Lib.JSONStore
{
    public class JSONStoreQuery
    {
        public JArray lessThan { get; set; }
        public JArray lessOrEqualThan { get; set; }
        public JArray greaterThan { get; set; }
        public JArray greaterOrEqualThan { get; set; }
        public JArray like { get; set; }
        public JArray notLike { get; set; }
        public JArray rightLike { get; set; }
        public JArray leftLike { get; set; }
        public JArray notRightLike { get; set; }
        public JArray notLeftLike { get; set; }
        public JArray equal { get; set; }
        public JArray notEqual { get; set; }
        public JArray inside { get; set; }
        public JArray notInside { get; set; }
        public JArray between { get; set; }
        public JArray notBetween { get; set; }
        public JArray ids { get; set; }
    }
}
