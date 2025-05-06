using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

public class Parser : MonoBehaviour {

    public TextAsset fileName;

	void Start () {
        // Ayuda TextAsset: https://docs.unity3d.com/ScriptReference/TextAsset.html
        // Ayuda Unity de String https://docs.unity3d.com/ScriptReference/String.html
        // Ayuda MSDN de String https://docs.microsoft.com/en-us/dotnet/api/system.string?redirectedfrom=MSDN&view=netframework-4.8
        // Ayuda MSDN de String.Split https://docs.microsoft.com/en-us/dotnet/api/system.string.split?view=netframework-4.8

        string[] textString = fileName.text.Split(new string[] { " ", "\n", "\r" }, StringSplitOptions.RemoveEmptyEntries);
        int numNodes = int.Parse(textString[0]);

        // NOTA: 
        // Para parsear números flotantes hay que tener en
        // cuenta el formato de número en el que está escrito.
        // Para ello hay que instanciar un objeto de la clase
        // CultureInfo que almacena información de localización. 
        // Los números con "." como separador decimal como 1.425
        // tienen localización de EEUU, "en-US".

        CultureInfo locale = new CultureInfo("en-US");
        float valor = float.Parse("1.425", locale);
    }

}
