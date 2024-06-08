using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
namespace GaussianSplatting.Runtime{
public class DisplayImageController : MonoBehaviour
{
    public GaussianSplatRenderer gaussianSplatRenderer;
    public Image displayImage;
    public 
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {

    }
    private void OnValidate()
    {
        // if (gaussianSplatRenderer != null)
        // {
        //     gaussianSplatRenderer.SetImage(displayImage);
        // }
    }
}
}