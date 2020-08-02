using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;


class ObjectiveNetworker_Reciever
{
    private static MissionManager mManager = MissionManager.instance;
    public static Dictionary<int, VTEventTarget> scenarioActionsList = new Dictionary<int, VTEventTarget>();
    public static Dictionary<int, float> scenarioActionsListCoolDown = new Dictionary<int, float>();
    public static bool completeNext = false;
    public static void objectiveUpdate(int id, ObjSyncType status)
    {
        Debug.Log($"Doing objective update for id {id}.");
        if (mManager == null)
        {
            mManager = MissionManager.instance;
            if (mManager == null)
            {
                Debug.Log("MissionManager manager Null");
                return;
            }
        }
        MissionObjective obj = mManager.GetObjective(id);
        if (obj == null)
        {
            Debug.Log("obj was Null");
            return;
        }


        if (status == ObjSyncType.EMissionCompleted && !obj.completed)
        {
            Debug.Log("Completeing mission complete locally");
            completeNext = true;
            obj.CompleteObjective();
        }

        if (status == ObjSyncType.EMissionFailed && !obj.failed)
        {
            Debug.Log("failing mission complete locally");

            obj.FailObjective();
        }

        if (status == ObjSyncType.EMissionBegin && !obj.started)
        {
            Debug.Log("starting mission begin locally");

            obj.BeginMission();
        }

        if (status == ObjSyncType.EMissionCanceled && !obj.cancelled)
        {
            Debug.Log("starting mission cancel locally");

            obj.CancelObjective();
        }
    }
    public static void runScenarioAction(int hash)
    {
        if (scenarioActionsListCoolDown.ContainsKey(hash))
        {
            float currentTime = Time.unscaledTime;
            if (currentTime - scenarioActionsListCoolDown[hash] > 1.0f)
            {
                if (scenarioActionsList.ContainsKey(hash))
                {

                    scenarioActionsListCoolDown.Remove(hash);
                    scenarioActionsListCoolDown.Add(hash, currentTime);
                    scenarioActionsList[hash].Invoke();
                }
                else
                    Debug.Log("scenario error doesnt exsist");
            }
        }
        else

        {

            if (scenarioActionsList.ContainsKey(hash))
            {
                float currentTime = Time.unscaledTime;
                scenarioActionsList[hash].Invoke();
                scenarioActionsListCoolDown.Add(hash, currentTime);

            }
            else
                Debug.Log("secanrio error doesnt exsist");
        }

    }
}
