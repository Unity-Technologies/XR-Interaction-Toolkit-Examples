/**************************************************************************************************
 * Copyright : Copyright (c) Facebook Technologies, LLC and its affiliates. All rights reserved.
 *
 * Your use of this SDK or tool is subject to the Oculus SDK License Agreement, available at
 * https://developer.oculus.com/licenses/oculussdk/
 *
 * Unless required by applicable law or agreed to in writing, the Utilities SDK distributed
 * under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF
 * ANY KIND, either express or implied. See the License for the specific language governing
 * permissions and limitations under the License.
 **************************************************************************************************/

// Based off Unity forum post https://forum.unity.com/threads/how-to-change-the-name-of-list-elements-in-the-inspector.448910/

using UnityEngine;

namespace Oculus.Voice.Core.Utilities
{
    public class ArrayElementTitleAttribute : PropertyAttribute
    {
        public string varname;
        public string fallbackName;

        public ArrayElementTitleAttribute(string elementTitleVar = null, string fallbackName = null)
        {
            varname = elementTitleVar;
            this.fallbackName = fallbackName;
        }
    }
}
