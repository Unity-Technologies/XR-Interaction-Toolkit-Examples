using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class evelator_controll : MonoBehaviour
{
    public List<int> sequenceElevator = new List<int>();
    public bool Elevator_in_run;
    public float[] FloorHighs;
    public GameObject ElevatorCabin;
  

    public void AddTaskEve(string name)
    {
        if (name == "Button floor 1")
        {
            sequenceElevator.Add(0);

            if (!Elevator_in_run)
            {
                Elevator_in_run = true;
                EvelatorDo = StartCoroutine(executeTask());
            }

        }
        if (name == "Button floor 2")
        {
            sequenceElevator.Add(1);

            if (!Elevator_in_run)
            {
                Elevator_in_run = true;
                EvelatorDo = StartCoroutine(executeTask());
            }

        }
        if (name == "Button floor 3")
        {
            sequenceElevator.Add(2);

            if (!Elevator_in_run)
            {
                Elevator_in_run = true;
                EvelatorDo = StartCoroutine(executeTask());
            }

        }
        if (name == "Button floor 4")
        {
            sequenceElevator.Add(3);

            if (!Elevator_in_run)
            {
                Elevator_in_run = true;
                EvelatorDo = StartCoroutine(executeTask());
            }

        }
        if (name == "Button floor 5")
        {
            sequenceElevator.Add(4);

            if (!Elevator_in_run)
            {
                Elevator_in_run = true;
                EvelatorDo = StartCoroutine(executeTask());
            }

        }
        if (name == "Button floor 6")
        {
            sequenceElevator.Add(5);

            if (!Elevator_in_run)
            {
                Elevator_in_run = true;
                EvelatorDo = StartCoroutine(executeTask());
            }

        }
    }


    Coroutine EvelatorDo;
    Coroutine DoorOpening;
    Coroutine DoorClose;

    public bool Doors_finished;

    public IEnumerator executeTask()
    {
        continueTask:
        yield return new WaitForSeconds(0.005f);





        int toReach = sequenceElevator[0];


        if (sequenceElevator.Count > 0)
        {
            if (ElevatorCabin.transform.localPosition.y > FloorHighs[sequenceElevator[sequenceElevator.Count - 1]])
            {
                ElevatorCabin.transform.Translate(Vector3.down * Time.deltaTime);

                if (ElevatorCabin.transform.localPosition.y < (FloorHighs[sequenceElevator[sequenceElevator.Count - 1]] + 0.01f))

                {


                    DoorOpening = StartCoroutine(HandleDoorOpen(sequenceElevator[sequenceElevator.Count - 1]));
                    Doors_finished = false;
                    sequenceElevator.RemoveAt(sequenceElevator.Count - 1);
                    yield return new WaitWhile(() => !Doors_finished);

                }

            }
        }
        if (sequenceElevator.Count > 0)
        {
            if (ElevatorCabin.transform.localPosition.y < FloorHighs[sequenceElevator[sequenceElevator.Count - 1]])
            {
                ElevatorCabin.transform.Translate(Vector3.up * Time.deltaTime);

                if (ElevatorCabin.transform.localPosition.y > (FloorHighs[sequenceElevator[sequenceElevator.Count - 1]] - 0.01f))
                {

                    DoorOpening = StartCoroutine(HandleDoorOpen(sequenceElevator[sequenceElevator.Count - 1]));
                    Doors_finished = false;
                    sequenceElevator.RemoveAt(sequenceElevator.Count - 1);
                    yield return new WaitWhile(() => !Doors_finished);
                }

            }
        }


        ChangeFloorNumbers();


        if (sequenceElevator.Count == 0)
        {
            Elevator_in_run = false;
            StopCoroutine(EvelatorDo);

        }
        if (sequenceElevator.Count > 0)
        {
            goto continueTask;
        }

    }


    public GameObject[] FloorNumbers;
    public int CurrentFloorNumber;

    public void ChangeFloorNumbers()
    {


        if ((ElevatorCabin.transform.localPosition.y) < FloorHighs[0] + 0.1f && (ElevatorCabin.transform.localPosition.y) > FloorHighs[0] - 0.1f)
        {
            CurrentFloorNumber = 1;
        }
        if ((ElevatorCabin.transform.localPosition.y) < FloorHighs[1] + 0.1f && (ElevatorCabin.transform.localPosition.y) > FloorHighs[1] - 0.1f)
        {
            CurrentFloorNumber = 2;
        }
        if ((ElevatorCabin.transform.localPosition.y) < FloorHighs[2] + 0.1f && (ElevatorCabin.transform.localPosition.y) > FloorHighs[2] - 0.1f)
        {
            CurrentFloorNumber = 3;
        }
        if ((ElevatorCabin.transform.localPosition.y) < FloorHighs[3] + 0.1f && (ElevatorCabin.transform.localPosition.y) > FloorHighs[3] - 0.1f)
        {
            CurrentFloorNumber = 4;
        }
        if ((ElevatorCabin.transform.localPosition.y) < FloorHighs[4] + 0.1f && (ElevatorCabin.transform.localPosition.y) > FloorHighs[4] - 0.1f)
        {
            CurrentFloorNumber = 5;
        }
        if ((ElevatorCabin.transform.localPosition.y) < FloorHighs[5] + 0.1f && (ElevatorCabin.transform.localPosition.y) > FloorHighs[5] - 0.1f)
        {
            CurrentFloorNumber = 6;
        }


        foreach (GameObject Numberassemble in FloorNumbers)
        {


            List<GameObject> Numbers_in_holder = new List<GameObject>();





            for (int i = 0; i < Numberassemble.transform.childCount; i++)
            {


                Numbers_in_holder.Add(Numberassemble.transform.GetChild(i).gameObject);


            }

            foreach (GameObject numberA in Numbers_in_holder)
            {
                numberA.SetActive(false);
                Numbers_in_holder[CurrentFloorNumber - 1].SetActive(true);
            }



        }


    }

    public float DoorOpenTime;

    public GameObject[] Door_outside_left; public float[] Door_outside_left_close_value; public float[] Door_outside_left_open_value;
    public GameObject[] Door_outside_right; public float[] Door_outside_right_close_value; public float[] Door_outside_right_open_value;

    public GameObject Door_inside_right; public float Door_inside_right_close_value; public float Door_inside_right_open_value;
    public GameObject Door_inside_left; public float Door_inside_left_close_value; public float Door_inside_left_open_value;





    public IEnumerator HandleDoorOpen(int WhichFloor)
    {
        Repeating:
        yield return new WaitForSeconds(0.005f);




        Door_inside_left.transform.localPosition = Vector3.Lerp(new Vector3(Door_inside_left.transform.localPosition.x, Door_inside_left.transform.localPosition.y, Door_inside_left.transform.localPosition.z), new Vector3(Door_inside_left_open_value, Door_inside_left.transform.localPosition.y, Door_inside_left.transform.localPosition.z), DoorOpenTime * Time.deltaTime);
        Door_inside_right.transform.localPosition = Vector3.Lerp(new Vector3(Door_inside_right.transform.localPosition.x, Door_inside_right.transform.localPosition.y, Door_inside_right.transform.localPosition.z), new Vector3(Door_inside_right_open_value, Door_inside_right.transform.localPosition.y, Door_inside_right.transform.localPosition.z), DoorOpenTime * Time.deltaTime);


        Door_outside_left[WhichFloor].transform.localPosition = Vector3.Lerp(new Vector3(Door_outside_left[WhichFloor].transform.localPosition.x, Door_outside_left[WhichFloor].transform.localPosition.y, Door_outside_left[WhichFloor].transform.localPosition.z), new Vector3(Door_outside_left_open_value[WhichFloor], Door_outside_left[WhichFloor].transform.localPosition.y, Door_outside_left[WhichFloor].transform.localPosition.z), DoorOpenTime * Time.deltaTime);
        Door_outside_right[WhichFloor].transform.localPosition = Vector3.Lerp(new Vector3(Door_outside_right[WhichFloor].transform.localPosition.x, Door_outside_right[WhichFloor].transform.localPosition.y, Door_outside_right[WhichFloor].transform.localPosition.z), new Vector3(Door_outside_right_open_value[WhichFloor], Door_outside_right[WhichFloor].transform.localPosition.y, Door_outside_right[WhichFloor].transform.localPosition.z), DoorOpenTime * Time.deltaTime);


        if ((Door_inside_left.transform.localPosition.x - 0.001f) > Door_inside_left_open_value)
        {
            goto Repeating;
        }
        else
        {
            
            yield return new WaitForSeconds(5);

            DoorClose = StartCoroutine(HandleDoorClose(WhichFloor));
            StopCoroutine(DoorOpening);
        }
    }
    public IEnumerator HandleDoorClose(int WhichFloor)
    {
        Repeating:
        yield return new WaitForSeconds(0.005f);

        Door_inside_left.transform.localPosition = Vector3.Lerp(new Vector3(Door_inside_left.transform.localPosition.x, Door_inside_left.transform.localPosition.y, Door_inside_left.transform.localPosition.z), new Vector3(Door_inside_left_close_value, Door_inside_left.transform.localPosition.y, Door_inside_left.transform.localPosition.z), DoorOpenTime * Time.deltaTime);
        Door_inside_right.transform.localPosition = Vector3.Lerp(new Vector3(Door_inside_right.transform.localPosition.x, Door_inside_right.transform.localPosition.y, Door_inside_right.transform.localPosition.z), new Vector3(Door_inside_right_close_value, Door_inside_right.transform.localPosition.y, Door_inside_right.transform.localPosition.z), DoorOpenTime * Time.deltaTime);


       Door_outside_left[WhichFloor].transform.localPosition = Vector3.Lerp(new Vector3(Door_outside_left[WhichFloor].transform.localPosition.x, Door_outside_left[WhichFloor].transform.localPosition.y, Door_outside_left[WhichFloor].transform.localPosition.z), new Vector3(Door_outside_left_close_value[WhichFloor], Door_outside_left[WhichFloor].transform.localPosition.y, Door_outside_left[WhichFloor].transform.localPosition.z), DoorOpenTime * Time.deltaTime);
       Door_outside_right[WhichFloor].transform.localPosition = Vector3.Lerp(new Vector3(Door_outside_right[WhichFloor].transform.localPosition.x, Door_outside_right[WhichFloor].transform.localPosition.y, Door_outside_right[WhichFloor].transform.localPosition.z), new Vector3(Door_outside_right_close_value[WhichFloor], Door_outside_right[WhichFloor].transform.localPosition.y, Door_outside_right[WhichFloor].transform.localPosition.z), DoorOpenTime * Time.deltaTime);




        
           
        
        if ((Door_inside_left.transform.localPosition.x + 0.00001f) > Door_inside_left_close_value)
        {
              Doors_finished = true;
              StopCoroutine(DoorClose);
        }
            goto Repeating;
    }
}