using System.Xml;
using UnityEngine;

namespace HikersMod.Components
{
    public class SuperBoostNote : MonoBehaviour
    {
        AssetBundle textAssets = HikersMod.Instance._textAssets;

        public void Start()
        {
            CharacterDialogueTree dialogueTree = gameObject.GetComponentInChildren<CharacterDialogueTree>();
            InteractVolume interactVolume = gameObject.GetComponentInChildren<InteractVolume>();
            GameObject page = gameObject.transform.Find("plaque_paper_1 (1)").gameObject;
            gameObject.name = "HikersMod_SuperBoostNote";
            gameObject.transform.parent = GameObject.Find("Ship_Body").transform;
            gameObject.transform.localPosition = new Vector3(1.3477f, 2.324f, 1.5702f);
            gameObject.transform.localRotation = Quaternion.Euler(89.9441f, 224.0488f, 0f);
            Destroy(gameObject.transform.Find("plaque_paper_1 (2)").gameObject);
            Destroy(gameObject.transform.Find("plaque_paper_1 (3)").gameObject);
            gameObject.GetComponentInChildren<MeshRenderer>().enabled = true;
            interactVolume.transform.position = page.transform.position;
            interactVolume.transform.localScale = new Vector3(0.75f, 0.75f, 0.75f);
            interactVolume.GetComponent<InteractReceiver>()._usableInShip = true;
            gameObject.SetActive(HikersMod.Instance._isSuperBoostEnabled);
            HikersMod.Instance._superBoostNote = gameObject;
            HikersMod.Instance.ModHelper.Events.Unity.FireInNUpdates(() =>
            {
                dialogueTree._attentionPoint = page.transform;
                dialogueTree._characterName = "HikersMod_SuperBoostNote";
                interactVolume.enabled = true;
                interactVolume.EnableInteraction();
                TextAsset textAsset = textAssets.LoadAsset<TextAsset>("HikersMod_SuperBoostNote");
                dialogueTree.SetTextXml(textAsset);
                AddTranslations(textAsset.ToString());
                dialogueTree.OnDialogueConditionsReset();
            }, 60);
        }

        public void AddTranslations(string textAsset)
        {
            XmlDocument xmlDocument = new XmlDocument();
            xmlDocument.LoadXml(textAsset);
            XmlNode xmlNode = xmlDocument.SelectSingleNode("DialogueTree");
            XmlNodeList xmlNodeList = xmlNode.SelectNodes("DialogueNode");
            string NameField = xmlNode.SelectSingleNode("NameField").InnerText;
            var translationTable = TextTranslation.Get().m_table.theTable;
            translationTable[NameField] = NameField;
            foreach (object obj in xmlNodeList)
            {
                XmlNode xmlNode2 = (XmlNode)obj;
                var name = xmlNode2.SelectSingleNode("Name").InnerText;

                XmlNodeList xmlText = xmlNode2.SelectNodes("Dialogue/Page");
                foreach (object Page in xmlText)
                {
                    XmlNode pageData = (XmlNode)Page;
                    translationTable[name + pageData.InnerText] = pageData.InnerText;
                }
                xmlText = xmlNode2.SelectNodes("DialogueOptionsList/DialogueOption/Text");
                foreach (object Page in xmlText)
                {
                    XmlNode pageData = (XmlNode)Page;
                    translationTable[NameField + name + pageData.InnerText] = pageData.InnerText;

                }
            }
        }
    }
}
