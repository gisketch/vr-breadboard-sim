using System.Collections;
using System.Collections.Generic;
using Mirror;
using TMPro;
using UnityEngine;

public class ClassroomManager : NetworkBehaviour
{
    [SyncVar]
    public int currentStudentId = 1;

    public override void OnStartClient()
    {
        base.OnStartClient();
    }

    public override void OnStopClient()
    {
        base.OnStopClient();
    }

    [Command(ignoreAuthority = true)]
    public void CmdIncrementStudentId()
    {
        currentStudentId++;
    }

}
