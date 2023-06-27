/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * Licensed under the Oculus SDK License Agreement (the "License");
 * you may not use the Oculus SDK except in compliance with the License,
 * which is provided at the time of installation or download, or which
 * otherwise accompanies this software in either electronic or hard copy form.
 *
 * You may obtain a copy of the License at
 *
 * https://developer.oculus.com/licenses/oculussdk/
 *
 * Unless required by applicable law or agreed to in writing, the Oculus SDK
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using System.Collections.Generic;
using UnityEditor;

internal class OVRConfigurationTaskProcessorQueue
{
    public event Action<OVRConfigurationTaskProcessor> OnProcessorCompleted;

    private readonly Queue<OVRConfigurationTaskProcessor> _queue = new Queue<OVRConfigurationTaskProcessor>();

    public bool Busy => _queue.Count > 0;
    public bool Blocked => Busy && _queue.Peek().Blocking;

    public bool BlockedBy(OVRConfigurationTaskProcessor.ProcessorType processorType)
    {
        foreach (var processor in _queue)
        {
            if (processor.Type == processorType && processor.Blocking)
            {
                return true;
            }
        }

        return false;
    }

    public bool BusyWith(OVRConfigurationTaskProcessor.ProcessorType processorType)
    {
        foreach (var processor in _queue)
        {
            if (processor.Type == processorType)
            {
                return true;
            }
        }

        return false;
    }

    public void Request(OVRConfigurationTaskProcessor processor)
    {
        if (!OVRProjectSetup.Enabled.Value)
        {
            return;
        }

        Enqueue(processor);
    }

    private void Enqueue(OVRConfigurationTaskProcessor processor)
    {
        if (!Busy)
        {
            // If was empty, then register to editor update
            EditorApplication.update += Update;
        }

        // Enqueue
        _queue.Enqueue(processor);

        processor.OnRequested();

        if (processor.Blocking)
        {
            // In the case where the newly added processor is blocking
            // we'll make all the previously queued processor blocking as well
            foreach (var otherProcessor in _queue)
            {
                otherProcessor.Blocking = true;
            }

            // Force an update, this will be The blocking update
            Update();
        }
    }

    private void Dequeue(OVRConfigurationTaskProcessor processor)
    {
        // We should only dequeue the current processor
        if (processor != _queue.Peek())
        {
            return;
        }

        // Dequeue
        _queue.Dequeue();

        if (!Busy)
        {
            // Now that it is empty, unregister to editor update
            EditorApplication.update -= Update;
        }

        // Trigger specific callbacks
        processor.Complete();

        // Trigger global callbacks
        OnProcessorCompleted?.Invoke(processor);
    }

    private void Update()
    {
        var processor = _queue.Count > 0 ? _queue.Peek() : null;

        while (processor != null)
        {
            if (!processor.Started)
            {
                processor.Start();
            }

            processor.Update();

            if (processor.Completed)
            {
                Dequeue(processor);

                // Move to the next processor
                processor = _queue.Count > 0 ? _queue.Peek() : null;
            }

            if (!(processor?.Blocking ?? false))
            {
                // Is the processor is not blocking, we can stop the update until the next update call
                processor = null;
            }
        }
    }
}
