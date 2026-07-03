using System.Xml;

namespace AssimilationSoftware.Buildster.Core.Utils;


public static class ProjectInfo
{
    public static ProjectType GetProjectType(string projectFilePath)
    {
        if (!File.Exists(projectFilePath))
        {
            return ProjectType.None;
        }
        try
        {
            XmlDocument projectFile = new XmlDocument();
            projectFile.Load(projectFilePath);
            XmlNode node = projectFile.DocumentElement.SelectSingleNode("/Project");
            string attr = node.Attributes["Sdk"]?.InnerText;
            return string.IsNullOrWhiteSpace(attr) ? ProjectType.SDK : ProjectType.Framework;
        }
        catch (System.Exception)
        {
            return ProjectType.None;
        }
    }
}
