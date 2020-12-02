using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LitEngine.Net;
public class SampleNet : MonoBehaviour
{
    // Start is called before the first frame update
    SendData tetstdata = new SendData(10);
    private void Awake()
    {
        UDPNet.Init("127.0.0.1", 20236);
        UDPNet.ShowMsgLog(true);
        UDPNet.Connect();
        // TCPNet.Init("127.0.0.1", 20240);
        // TCPNet.ShowMsgLog(true);
        // TCPNet.Connect();
        tetstdata.AddInt(2);
        Application.runInBackground = true;
    }

    // Update is called once per frame

    void Update()
    {
        // if(TCPNet.IsConnect())
        // {
        //     TCPNet.Add(tetstdata);
        // }
        UDPNet.Add(tetstdata);
    }
}
