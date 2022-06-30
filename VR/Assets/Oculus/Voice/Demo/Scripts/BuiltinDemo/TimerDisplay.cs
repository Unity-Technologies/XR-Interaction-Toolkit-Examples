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

using UnityEngine;
using UnityEngine.UI;

namespace Oculus.Voice.Demo.BuiltInDemo
{
    public class TimerDisplay : MonoBehaviour
    {
        public TimerController timer;

        private Text _uiText;

        // Start is called before the first frame update
        void Start()
        {
            _uiText = GetComponent<Text>();
        }

        // Update is called once per frame
        void Update()
        {
            // Note: This is not optimized and you should avoid updating time each frame.
            _uiText.text = timer.GetFormattedTimeFromSeconds();
        }
    }
}
