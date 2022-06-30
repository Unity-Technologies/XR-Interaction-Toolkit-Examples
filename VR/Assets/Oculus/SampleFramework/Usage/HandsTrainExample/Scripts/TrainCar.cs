/************************************************************************************

Copyright (c) Facebook Technologies, LLC and its affiliates. All rights reserved.  

See SampleFramework license.txt for license terms.  Unless required by applicable law 
or agreed to in writing, the sample code is provided “AS IS” WITHOUT WARRANTIES OR 
CONDITIONS OF ANY KIND, either express or implied.  See the license for specific 
language governing permissions and limitations under the license.

************************************************************************************/

using UnityEngine;
using UnityEngine.Assertions;

namespace OculusSampleFramework
{
	public class TrainCar : TrainCarBase
	{
		[SerializeField] private TrainCarBase _parentLocomotive = null;
		[SerializeField] protected float _distanceBehindParent = 0.1f;

		public float DistanceBehindParentScaled
		{
			get { return scale * _distanceBehindParent; }
		}

		protected override void Awake()
		{
			base.Awake();
			Assert.IsNotNull(_parentLocomotive);
		}

		public override void UpdatePosition()
		{
			Distance = _parentLocomotive.Distance - DistanceBehindParentScaled;
			UpdateCarPosition();
			RotateCarWheels();
		}
	}
}
