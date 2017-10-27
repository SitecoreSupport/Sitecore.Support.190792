using Sitecore.ContentSearch;
using Sitecore.ContentSearch.Linq;
using Sitecore.ContentSearch.Linq.Utilities;
using Sitecore.ContentSearch.SearchTypes;
using Sitecore.Data;
using Sitecore.Data.Fields;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Xml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Web;

namespace Sitecore.Support.Data.Items
{
  public static class ItemUtil
  {
    public static void SetLayoutDetails(Item item, string sharedLayout, string finalLayout)
    {
      Assert.ArgumentNotNull(item, "item");
      Assert.ArgumentNotNull(sharedLayout, "sharedLayout");
      Assert.ArgumentNotNull(finalLayout, "finalLayout");
      string text = sharedLayout + finalLayout;

      sharedLayout = InvokeItemUtilCleanupLayoutValue(sharedLayout) as string;
      finalLayout = InvokeItemUtilCleanupLayoutValue(finalLayout) as string;
      using (new StatisticDisabler(StatisticDisablerState.ForItemsWithoutVersionOnly))
      {
        item.Editing.BeginEdit();
        Field field = item.Fields[FieldIDs.LayoutField];
        string xml = InvokeItemUtilCleanupLayoutValue(LayoutField.GetFieldValue(field)) as string;
        if (!XmlUtil.XmlStringsAreEqual(xml, sharedLayout))
        {
          LayoutField.SetFieldValue(field, sharedLayout);
        }
        if (!item.RuntimeSettings.TemporaryVersion)
        {
          Field field2 = item.Fields[FieldIDs.FinalLayoutField];
          LayoutField.SetFieldValue(field2, finalLayout, sharedLayout);
        }
        item.Editing.EndEdit();
      }
      if (item.Name == "__Standard Values")
      {
        Sitecore.Support.Data.Items.ItemUtil.CleanupInheritedItems(item);
      }
      Log.Audit(typeof(ItemUtil), "Set layout details: {0}, layout: {1}", new string[]
      {
        AuditFormatter.FormatItem(item),
        text
      });
    }

    private static void CleanupInheritedItems(Item item)
    {
      Item[] array = GetInheritedItems(item);
      if (array == null)
      {
        return;
      }
      using (new StatisticDisabler(StatisticDisablerState.ForItemsWithoutVersionOnly))
      {
        Item[] array2 = array;
        for (int i = 0; i < array2.Length; i++)
        {
          Item item2 = array2[i];
          Field field = item2.Fields[FieldIDs.LayoutField];
          Field field2 = item2.Fields[FieldIDs.FinalLayoutField];
          using (new EditContext(item2))
          {
            if (field.HasValue)
            {
              string text = LayoutField.GetFieldValue(field);
              text = InvokeItemUtilCleanupLayoutValue(text) as string;
              LayoutField.SetFieldValue(field, text);
            }
            if (field2.HasValue)
            {
              string text2 = LayoutField.GetFieldValue(field2);
              text2 = InvokeItemUtilCleanupLayoutValue(text2) as string;
              LayoutField.SetFieldValue(field2, text2);
            }
          }
        }
      }
    }

    private static Item[] GetInheritedItems(Item item)
    {
      var path = "/sitecore/content";
      Item[] items = new Item[0];
      using (IProviderSearchContext searchContext = ContentSearchManager.GetIndex($"sitecore_{item.Database.Name}_index").CreateSearchContext())
      {
        var predicate = PredicateBuilder.True<SearchResultItem>()
          .And(searchItem => searchItem.TemplateId == item.TemplateID);

        var searchItems = searchContext.GetQueryable<SearchResultItem>()
         .Where(predicate)
         .Where(searchItem => searchItem["_latestversion"].Equals("1"))
         .Filter(searchItem => searchItem.Path.StartsWith(path))
         .Filter(searchItem => searchItem.Language == Sitecore.Context.Language.Name).ToArray();

        items = new Item[searchItems.Count()];
        for (int i = 0; i < searchItems.Count(); i++)
        {
          items[i] = item.Database.GetItem(searchItems[i].ItemId);
        }
      }

      return items;
    }

    private static object InvokeItemUtilCleanupLayoutValue(params object[] parameters)
    {
      var itemUtilType = typeof(Sitecore.Data.Items.ItemUtil);
      var flags = BindingFlags.NonPublic | BindingFlags.Static;

      return itemUtilType.GetMethod("CleanupLayoutValue", flags).Invoke(null, parameters);
    }
  }
}