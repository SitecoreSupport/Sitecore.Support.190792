using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Sitecore.Shell.Framework.Commands;
using Sitecore.Web.UI.Sheer;
using Sitecore.Data.Items;
using Sitecore.Data;
using Sitecore.Diagnostics;
using Sitecore.Shell.Applications.Dialogs.LayoutDetails;
using Sitecore.Configuration;
using Sitecore.Globalization;
using Sitecore.Text;

namespace Sitecore.Support.Shell.Framework.Commands
{
  public class SetLayoutDetails : Sitecore.Shell.Framework.Commands.SetLayoutDetails
  {
    protected override void Run(ClientPipelineArgs args)
    {
      Assert.ArgumentNotNull(args, "args");
      if (!SheerResponse.CheckModified())
      {
        return;
      }
      if (args.IsPostBack)
      {
        if (args.HasResult)
        {
          Database database = Factory.GetDatabase(args.Parameters["database"]);
          Assert.IsNotNull(database, "Database \"" + args.Parameters["database"] + "\" not found.");
          Item item = database.GetItem(ID.Parse(args.Parameters["id"]), Language.Parse(args.Parameters["language"]), Sitecore.Data.Version.Parse(args.Parameters["version"]));
          Assert.IsNotNull(item, "item");
          LayoutDetailsDialogResult layoutDetailsDialogResult = LayoutDetailsDialogResult.Parse(args.Result);
          Sitecore.Support.Data.Items.ItemUtil.SetLayoutDetails(item, layoutDetailsDialogResult.Layout, layoutDetailsDialogResult.FinalLayout);
          if (layoutDetailsDialogResult.VersionCreated)
          {
            Context.ClientPage.SendMessage(this, string.Concat(new object[]
            {
          "item:versionadded(id=",
          item.ID,
          ",version=",
          item.Version,
          ",language=",
          item.Language,
          ")"
            }));
            return;
          }
        }
      }
      else
      {
        UrlString urlString = new UrlString(UIUtil.GetUri("control:LayoutDetails"));
        urlString.Append("id", args.Parameters["id"]);
        urlString.Append("la", args.Parameters["language"]);
        urlString.Append("vs", args.Parameters["version"]);
        SheerResponse.ShowModalDialog(urlString.ToString(), "650px", string.Empty, string.Empty, true);
        args.WaitForPostBack();
      }
    }
  }
}