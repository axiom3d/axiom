using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using CodePlex.WebServices.Client;
using CodePlex.WebServices.Client.Tasks;
using Microsoft.Build.Framework;

namespace Axiom.Build.Tasks
{
public class UploadFiles : ReleaseTaskBase
{
    // Fields
    private ITaskItem[] releaseFileItems;

    // Methods
    public override bool Execute()
    {
        base.Execute();
        try
        {
            base.LogMessage("Uploading files to release '{0}' as user '{1}'", new object[] { base.releaseName, base.userName });
            IList<ReleaseFile> releaseFiles = this.GetReleaseFiles();
			base.releaseService.Timeout = Timeout.Infinite;
            base.releaseService.UploadReleaseFiles(base.projectName, base.releaseName, releaseFiles, this.RecommendedFileName);
            return true;
        }
        catch (Exception ex)
        {
            base.LogError("Unable to complete UploadReleaseFiles Task. [{0}]: {1}", new object[] { ex.GetType(), ex.Message });
            return false;
        }
    }

    private List<ReleaseFile> GetReleaseFiles()
    {
        List<ReleaseFile> fileList = new List<ReleaseFile>();
        foreach (ITaskItem item in this.releaseFileItems)
        {
            string filePath = null;
            try
            {
                if (item.ItemSpec.Length != 0)
                {
                    filePath = item.GetMetadata("FullPath");
                    using (Stream fileStream = File.OpenRead(filePath))
                    {
                        ReleaseFile file = new ReleaseFile(ReleaseTaskBase.GetMetadata(item, "Name"), ReleaseTaskBase.GetMetadata(item, "MimeType"), item.GetMetadata("Filename") + item.GetMetadata("Extension"), fileStream, base.GetEnumValue<ReleaseFileType>(ReleaseTaskBase.GetMetadata(item, "FileType") ?? "RuntimeBinary"));
                        fileList.Add(file);
                        base.LogMessage("  \"{0}\" as \"{1}\"", new object[] { file.FileName, file.Name ?? Path.GetFileName(file.FileName) } );
                    }
                }
            }
            catch (ArgumentException)
            {
                throw;
            }
            catch (Exception)
            {
                if (filePath != null)
                {
                    base.LogError("Unable to open file: {0}", new object[] { filePath });
                }
            }
        }
        return fileList;
    }

    // Properties
    public string RecommendedFileName
    {
        get;
        set;
    }

    [Required]
    public ITaskItem[] ReleaseFiles
    {
        get
        {
            return this.releaseFileItems;
        }
        set
        {
            this.releaseFileItems = value;
        }
    }
}

}
