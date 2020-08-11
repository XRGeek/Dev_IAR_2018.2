﻿/*
 * ============================================================================== 
 * Copyright (c) 2019-2020 EmachineLabs Corp. All Rights Reserved.
 * 
 * @Author : Jitender Hooda 
 * 
 ==============================================================================
 */
using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Linq;
using System.Linq;
using System.Xml.XPath;
using System.Web;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Net;
using System.IO;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using System.Text;
using UnityEngine.Events;

public class TestButtonScript : MonoBehaviour
{
    public static Dictionary<string, BlocklyReference> blocklyReferences = new Dictionary<string, BlocklyReference>();
    public static Dictionary<string, BlocklyReference> blocklyReferencesSaved = new Dictionary<string, BlocklyReference>();
    public static Dictionary<string, BlocklyReference> blocklyReferencesGlobal = new Dictionary<string, BlocklyReference>();
    Dictionary<string, List<CodeEventBlocks>> eventCodeblockMapping
        = new Dictionary<string, List<CodeEventBlocks>>();
    public XElement baseElement;
    static Dictionary<string, string> queryResultsMapping = new Dictionary<string, string>();
    public static string popupId = null;
    public string popupMessage = "";
    public string popupResponse = "";
    public bool firstExecution = false;
    public UnityEvent flipped;

    XmlNamespaceManager nsmgr = new XmlNamespaceManager(new NameTable());
    XNamespace ns = "https://developers.google.com/blockly/xml";

    GameObject eventGameObject;
    string eventType;
    BlockFactory factory;

    // Use this for initialization
    void Start()
    {

    }

    public void clicked(GameObject gameobject)//put GameObject
    {
        //PopupUtilities.makePopupTable(null, TextBlockImpl.getHardCodeStringJason(), true, null);
        //PopupUtilities.makePopupYesNo(this.gameObject, "maja aa gaya", true, null);
        //DialogTrigger dt = gameobject.AddComponent<DialogueBox>(typeof(DialogTrigger)) as DialogTrigger;
        //DialogTrigger.Init();
        //factory = new BlockFactory();
        if ((gameobject.GetComponentInChildren<Text>().text.ToLower().Equals("back")) ^
                 (gameobject.GetComponentInChildren<Text>().text.ToLower().Equals("<<")))
        {
            Back2CloudScene();
        }
        //this.eventGameObject = gameobject;
        //this.eventType = "CLICK";
        //nsmgr.AddNamespace("prefix", "https://developers.google.com/blockly/xml");
        //blocklyReferences = new Dictionary<string, BlocklyReference>(); //TODO LAX find better solve
        //eventCodeblockMapping = new Dictionary<string, List<CodeEventBlocks>>(); //TODO LAX find better solve
        //parseXML(null, null, null, null);
        //foreach(KeyValuePair<string, BlocklyReference> item in blocklyReferences)
        //{
        //    if (blocklyReferencesSaved.ContainsKey(item.Key))
        //    {
        //        saveExistingBlockyReference(item.Key);
        //    } else
        //    {
        //        blocklyReferencesSaved.Add(item.Key, item.Value);
        //    }
        //}
        BlocklyEvents bEvents = new BlocklyEvents();
        bEvents.eventType = "Click";
        bEvents.clickedEvent(gameObject);
    }

    public void ok()
    {
        UnityEngine.SceneManagement.SceneManager.LoadSceneAsync("3-CloudReco");
        SceneManager.UnloadSceneAsync("UIScene");
    }

    public void Back2CloudScene()
    {
        UnityEngine.SceneManagement.SceneManager.LoadSceneAsync("3-CloudReco");
    }

    public void executePopUpNextBlock(string message, string response)
    {
        if (popupId != null)
        {
            factory = new BlockFactory();
            this.popupMessage = message;
            this.popupResponse = response;
            string xml = getWorkspaceString();
            Debug.Log("<color=purple> @@@@ Workspace XML loaded again for popup : </color>" + xml);
            XElement element = XElement.Parse(xml);
            baseElement = element;
            element = element.Descendants(ns + "block").Where(child => child.Attribute("id").Value.Equals(popupId)).FirstOrDefault();
            parseBlock(element);
        }
    }

    public void executeFirstTimeBlocky()
    {
        firstExecution = true;
        factory = new BlockFactory();
        string xml = getWorkspaceString();
        Debug.Log("<color=purple> @@@@ Workspace XML loaded for first time execution : </color>" + xml);
        XElement element = XElement.Parse(xml);
        baseElement = element;
        var elementsToProcess = element.Elements(ns + "block");
        foreach(XElement x in elementsToProcess)
        {
            parseBlock(x);
        }
    }

    private object parseXML(string idType, string codeBlockType, string codeBlockName, XElement element)
    {
        object obj = new object();
        if (element == null)
        {
            //its first time of this method, lets get workspace string and get XDocument doc
            string xml = getWorkspaceString();
            Debug.Log("<color=purple> @@@@ Workspace XML loaded : </color>" + xml);
            element = XElement.Parse(xml);
            baseElement = element;

            //lets find the code blocks that needs to be executed as part of this element
            getCodeBlocks(element);
            if (eventCodeblockMapping.ContainsKey(this.eventGameObject.name))
            {
                CodeEventBlocks ceb = eventCodeblockMapping[this.eventGameObject.name].Find(x => x.eventName.Equals(this.eventType));
                foreach (CodeBlocks cb in ceb.codeBlocks)
                {
                    obj = parseXML(cb.idType.ToString(), cb.codeblockType, cb.value, element);
                }
            }
            return obj;
        }
        else
        {
            if (idType == null || idType.Equals(BlockIdentificationType.Name.ToString()))
            {
                obj = executeBlockType(BlockIdentificationType.Name.ToString(), codeBlockType, codeBlockName, element);
            }
            else if (idType.Equals(BlockIdentificationType.Id.ToString()))
            {
                if (codeBlockName != null && codeBlockName.Length > 1)
                {
                    element = element.Descendants(ns + "block").Where(child => child.Attribute("id").Value.Equals(codeBlockName)).FirstOrDefault();
                    Debug.Log("<color=green> $$$$$$$$$$$$$$$$$ element  : </color>" + element);
                }
                else
                {
                    //What to do here?
                }
                if (element != null)
                {
                    obj = executeBlockType(BlockIdentificationType.Id.ToString(), codeBlockType, codeBlockName, element);
                }
            }
            else if (idType.Equals("type"))
            {
                //not sure what to do here yet
            }

        }
        return obj;
    }

    public object executeBlockType(string idType, string codeBlockType, string codeBlockName, XElement element)
    {
        //return factory.parse(this, (BlockCategory)Enum.Parse(typeof(BlockCategory), codeBlockType), codeBlockType, codeBlockName, element);
        return factory.parse(null, (BlockCategory)Enum.Parse(typeof(BlockCategory), codeBlockType), codeBlockType, codeBlockName, element);
    }

    private void getCodeBlocks(XElement element)
    {
        string sss = element.ToString();
        IEnumerable<XElement> elements = element.Descendants(ns + "block")
            .Where(child => child.Attribute("type").Value.Equals("simpleEvent"));
        foreach (XElement ele in elements)
        {
            XElement doStatements = BlocklyUtil.applyNameSpace(ele).Descendants(BlocklyUtil.ns + "statement")
                .Where(child => child.Attribute("name").Value.StartsWith("DO", StringComparison.Ordinal)).FirstOrDefault();
            string eleId = ele.Descendants(ns + "field")
            .Where(child => child.Attribute("name").Value.Equals("ElementId")).FirstOrDefault().Value;
            string eleEventId = ele.Descendants(ns + "field")
            .Where(child => child.Attribute("name").Value.Equals("EventId")).FirstOrDefault().Value;

            if (eleId != null && eleEventId != null)
            {
                if (!eventCodeblockMapping.ContainsKey(eleId))
                {
                    CodeEventBlocks ceb = new CodeEventBlocks();
                    ceb.elementId = eleId;
                    ceb.eventName = eleEventId;
                    ceb.codeBlocks = new List<CodeBlocks>();

                    List<CodeEventBlocks> codeEventBlocks = new List<CodeEventBlocks>();
                    codeEventBlocks.Add(ceb);
                    eventCodeblockMapping.Add(eleId, codeEventBlocks);
                }
                else
                {
                    List<CodeEventBlocks> codeEventBlocks = eventCodeblockMapping[eleId];
                    if (!codeEventBlocks.Any(n => n.eventName.Equals(eleEventId)))
                    {
                        CodeEventBlocks ceb = new CodeEventBlocks();
                        ceb.elementId = eleId;
                        ceb.eventName = eleEventId;
                        ceb.codeBlocks = new List<CodeBlocks>();
                        codeEventBlocks.Add(ceb);
                    }
                }
                XElement next = ele.Element(ns + "next");
                if (next == null && doStatements != null)
                {
                    doStatements = BlocklyUtil.applyNameSpace(doStatements).Element(ns + "statement").Element(ns + "block");
                    if (doStatements != null)
                    {
                        parseStatementCodeBlock(doStatements, eventCodeblockMapping[eleId].Find(x => x.eventName.Equals(eleEventId)));
                    }
                }
                if (next != null)
                {
                    List<CodeEventBlocks> cc = eventCodeblockMapping[eleId];

                    parseNextCodeBlock(next, eventCodeblockMapping[eleId].Find(x => x.eventName.Equals(eleEventId)));
                }

            }
        }
    }

    private void parseNextCodeBlock(XElement block, CodeEventBlocks codeEventBlock)
    {
        block = getXElementWithNameSpace(block);
        XElement nextBlock = block.Element(ns + "next").Element(ns + "block");
        if (nextBlock != null)
        {
            string blockType = nextBlock.Attribute("type").Value;
            switch (blockType)
            {
                case "procedures_callnoreturn":
                    string name = nextBlock.Element(ns + "mutation").Attribute("name").Value;
                    CodeBlocks cb = new CodeBlocks();
                    cb.idType = BlockIdentificationType.Name;
                    cb.value = name;
                    cb.codeblockType = blockType;
                    codeEventBlock.codeBlocks.Add(cb);
                    XElement next = nextBlock.Element(ns + "next");
                    if (next != null)
                    {
                        parseNextCodeBlock(next, codeEventBlock);
                    }
                    break;
                default:
                    CodeBlocks cbd = new CodeBlocks();
                    cbd.codeblockType = blockType;
                    cbd.idType = BlockIdentificationType.Id;
                    cbd.value = nextBlock.Attribute("id").Value;
                    codeEventBlock.codeBlocks.Add(cbd);
                    break;
            }
        }
    }

    private void parseStatementCodeBlock(XElement block, CodeEventBlocks codeEventBlock)
    {
        string blockType = block.Attribute("type").Value;
        switch (blockType)
        {
            case "procedures_callnoreturn":
                string name = BlocklyUtil.applyNameSpace(block).Element(ns + "block").Element(ns + "mutation").Attribute("name").Value;
                CodeBlocks cb = new CodeBlocks();
                cb.idType = BlockIdentificationType.Name;
                cb.value = name;
                cb.codeblockType = blockType;
                codeEventBlock.codeBlocks.Add(cb);
                XElement next = BlocklyUtil.applyNameSpace(block).Element(ns + "block").Element(ns + "next");
                if (next != null)
                {
                    parseNextCodeBlock(next, codeEventBlock);
                }
                break;
            default:
                CodeBlocks cbd = new CodeBlocks();
                cbd.codeblockType = blockType;
                cbd.idType = BlockIdentificationType.Id;
                cbd.value = block.Attribute("id").Value;
                codeEventBlock.codeBlocks.Add(cbd);
                break;
        }
    }

    private string getWorkspaceString()
    {
        string workspaceXML;
        string uiApiURL = GlobalVariables.UI_WorkspaceAPI + GlobalVariables.VUFORIA_UNIQUE_ID;
        string secondaryImagePath = Path.Combine(Application.persistentDataPath, GlobalVariables.VUFORIA_UNIQUE_ID, GlobalVariables.VUFORIA_UNIQUE_ID + ".xml");
        if (File.Exists(secondaryImagePath))
        {
            workspaceXML = File.ReadAllText(secondaryImagePath);
        }
        else
        {
            var directResponse = WebFunctions.Get(uiApiURL);
            workspaceXML = directResponse.text;
        }
        Debug.Log("<color=green> $$$$$$$$$$$$$$$$$ getWorkspaceString  : </color>" + workspaceXML);
        return workspaceXML;
    }

    public object parseBlock(XElement block)
    {
        object obj = new object();
        string ss = block.ToString();
        XAttribute attr = block.Attribute("type");
        if (block != null && block.Attribute("type") != null)
        {
            string type = block.Attribute("type").Value;
            if (block.Element(ns + "field") != null)
            {
                string fieldName = block.Element(ns + "field").Attribute("name").Value;
                obj = parseXML(null, type, fieldName, block);
            }
            else if (block.Element(ns + "mutation") != null &&
                block.Element(ns + "mutation").Attribute("name") != null)
            {
                string fieldName = block.Element(ns + "mutation").Attribute("name").Value;
                obj = parseXML(null, type, fieldName, block);
            }
            else
            {
                obj = parseXML(null, type, null, block);
            }
        }
        return obj;
    }

    public void parseNextBlock(XElement block)
    {
        block = getXElementWithNameSpace(block);

        XElement next = block.Element(ns + "block");//.Element(ns + "next");
        if (next != null)
        {
            //next = getXElementWithNameSpace(next);
            //next = next.Element(ns + "next");
            next = block.Element(ns + "block").Element(ns + "next");
        }
        //XElement next = block.Element(ns + "block").Element(ns + "next");

        //XElement next = block.Descendants(BlocklyUtil.ns + "next").FirstOrDefault();

        if (next != null)
        {
            XElement nextBlock = next.Element(ns + "block");
            if (next != null)
            {
                parseBlock(nextBlock);
            }
        }
    }

    private XElement getXElementWithNameSpace(XElement element)
    {
        string s = element.ToString().Trim();
        if (!s.StartsWith("<xml", StringComparison.Ordinal))
        {
            s = "<xml xmlns=\"https://developers.google.com/blockly/xml\"> \n" + s + "\n</xml>";
            element = XElement.Parse(s);
        }
        return element;
    }

    private void saveExistingBlockyReference(string key)
    {
        string type = blocklyReferences[key].type;
        object obj = blocklyReferences[key].value;
        if (type != null && (type.Equals("list") || type.Equals("ARQuery") || type.Equals("ARQueryAll")))
        {
            //((List<object>)(blocklyReferencesSaved[key].value)).Append(BlocklyUtil.getListFromObj(obj));
            string savedType = blocklyReferencesSaved[key].type;
            if (savedType != null && (savedType.Equals("list") || savedType.Equals("ARQuery") || savedType.Equals("ARQueryAll")))
            {
                List<object> savedList = BlocklyUtil.getListFromObj(blocklyReferencesSaved[key]);
                List<object> toBeSaved = BlocklyUtil.getListFromObj(obj);
                foreach (object save in toBeSaved)
                {
                    if (savedList == null)
                    {
                        savedList = new List<object>();
                    }
                    savedList.Add(save);
                }
                //savedList.Add(obj);
                blocklyReferencesSaved[key].value = savedList;
            } else
            {
                blocklyReferencesSaved[key].value = obj;
            }
            
        }
        else
        {
            blocklyReferencesSaved[key].value = obj;
        }
    }
}