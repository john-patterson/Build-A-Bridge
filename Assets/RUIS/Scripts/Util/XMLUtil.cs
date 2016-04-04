/*****************************************************************************

Content    :   Basic XML utilities
Authors    :   Mikael Matveinen
Copyright  :   Copyright 2013 Tuukka Takala, Mikael Matveinen. All Rights reserved.
Licensing  :   RUIS is distributed under the LGPL Version 3 license.

******************************************************************************/

using UnityEngine;
using System.Collections;
using System.IO;
using System.Xml;
using System.Xml.Schema;
using System.Text;

public class XMLUtil {
    public static void SaveXmlToFile(string xmlFilename, XmlDocument xmlDocument)
    {
        FileStream xmlFileStream = File.Open(xmlFilename, FileMode.Create);
        StreamWriter streamWriter = new StreamWriter(xmlFileStream);
        xmlDocument.Save(streamWriter);
        streamWriter.Flush();
        streamWriter.Close();
        xmlFileStream.Close();
    }

    public static Vector2 GetVector2FromXmlNode(XmlNode xmlNode)
    {
        return new Vector2(float.Parse(xmlNode.Attributes["x"].Value), float.Parse(xmlNode.Attributes["y"].Value));
    }

    public static void WriteVector2ToXmlElement(XmlElement element, Vector2 vector)
    {
        element.SetAttribute("x", vector.x.ToString());
        element.SetAttribute("y", vector.y.ToString());
    }

    public static Vector3 GetVector3FromXmlNode(XmlNode xmlNode)
    {   
        return new Vector3(float.Parse(xmlNode.Attributes["x"].Value), float.Parse(xmlNode.Attributes["y"].Value), float.Parse(xmlNode.Attributes["z"].Value));
    }

    public static void WriteVector3ToXmlElement(XmlElement element, Vector3 vector)
    {
        element.SetAttribute("x", vector.x.ToString());
        element.SetAttribute("y", vector.y.ToString());
        element.SetAttribute("z", vector.z.ToString());
    }

    public static XmlDocument LoadAndValidateXml(string xmlFilename, TextAsset schemaFile)
    {
        return LoadAndValidateXml(xmlFilename, schemaFile, new ValidationEventHandler(BasicValidationHandler));
    }

    public static XmlDocument LoadAndValidateXml(string xmlFilename, TextAsset schemaFile, ValidationEventHandler validationEventHandler)
    {
        XmlTextReader textReader = null;
        XmlReader validatingReader = null;

        MemoryStream fs = null;
        try
        {
            textReader = new XmlTextReader(xmlFilename);
            XmlReaderSettings readerSettings = new XmlReaderSettings();
            readerSettings.ValidationType = ValidationType.Schema;
            readerSettings.ValidationEventHandler += validationEventHandler;

            fs = new MemoryStream(Encoding.ASCII.GetBytes(schemaFile.text));
            XmlSchema schema = XmlSchema.Read(fs, validationEventHandler);
            readerSettings.Schemas.Add(schema);

            validatingReader = XmlReader.Create(textReader, readerSettings); //new XmlValidatingReader(textReader);
 
            XmlDocument result = new XmlDocument();
            result.Load(validatingReader);

            Debug.Log("XML validation finished for " + xmlFilename + "!");

            fs.Close();
            validatingReader.Close();
            textReader.Close();

            return result;
        }
        catch (System.Exception e)
        {
			Debug.LogWarning(": Could not find file: " + xmlFilename + ". Error type: " +  e.GetType().ToString());
            if(fs != null) fs.Close();
            if(validatingReader != null) validatingReader.Close();
            if(textReader != null) textReader.Close();
            return null;
        }
    }

    private static void BasicValidationHandler(object sender, ValidationEventArgs args)
    {
        Debug.LogWarning("VALIDATION ERROR!");
        Debug.LogWarning(string.Format("\tSeverity:{0}", args.Severity));
        Debug.LogWarning(string.Format("\tMessage:{0}", args.Message));
    }
}
