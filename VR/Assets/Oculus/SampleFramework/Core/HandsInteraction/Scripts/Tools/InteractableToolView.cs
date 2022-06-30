/************************************************************************************

Copyright (c) Facebook Technologies, LLC and its affiliates. All rights reserved.  

See SampleFramework license.txt for license terms.  Unless required by applicable law 
or agreed to in writing, the sample code is provided “AS IS” WITHOUT WARRANTIES OR 
CONDITIONS OF ANY KIND, either express or implied.  See the license for specific 
language governing permissions and limitations under the license.

************************************************************************************/

namespace OculusSampleFramework
{
	/// <summary>
	/// The visual abstraction of an interactable tool.
	/// </summary>
	public interface InteractableToolView
	{
		InteractableTool InteractableTool { get; }
		void SetFocusedInteractable(Interactable interactable);

		bool EnableState { get; set; }
		// Useful if you want to tool to glow in case it interacts with an object.
		bool ToolActivateState { get; set; }
	}
}
