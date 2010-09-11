/* Yet Another Forum.NET
 * Copyright (C) 2003-2005 Bj�rnar Henden
 * Copyright (C) 2006-2010 Jaben Cargman
 * http://www.yetanotherforum.net/
 * 
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU General Public License
 * as published by the Free Software Foundation; either version 2
 * of the License, or (at your option) any later version.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 59 Temple Place - Suite 330, Boston, MA  02111-1307, USA.
 */
namespace YAF.Classes.Core
{
  #region Using

  using System;
  using System.IO;
  using System.Web;
  using System.Xml;

  using YAF.Classes.Data;
  using YAF.Classes.Interfaces;
  using YAF.Classes.Pattern;
  using YAF.Classes.Utils;

  #endregion

  /// <summary>
  /// The yaf theme.
  /// </summary>
  public class YafTheme : IYafTheme
  {
    #region Constants and Fields

    /// <summary>
    /// The _theme file.
    /// </summary>
    private string _themeFile;

    /// <summary>
    /// The _theme xml doc.
    /// </summary>
    private XmlDocument _themeXmlDoc;

    #endregion

    #region Constructors and Destructors

    /// <summary>
    /// Initializes a new instance of the <see cref="YafTheme"/> class.
    /// </summary>
    public YafTheme()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="YafTheme"/> class.
    /// </summary>
    /// <param name="newThemeFile">
    /// The new theme file.
    /// </param>
    public YafTheme([NotNull] string newThemeFile)
    {
      CodeContracts.ArgumentNotNull(newThemeFile, "newThemeFile");

      this.ThemeFile = newThemeFile;
    }

    #endregion

    #region Properties

    /// <summary>
    /// Gets or sets a value indicating whether LogMissingThemeItem.
    /// </summary>
    public bool LogMissingThemeItem { get; set; }

    /// <summary>
    /// Gets ThemeDir.
    /// </summary>
    public string ThemeDir
    {
      get
      {
        this.LoadThemeFile();
        return "{0}{1}/{2}/".FormatWith(
          YafForumInfo.ForumClientFileRoot, 
          YafBoardFolders.Current.Themes, 
          this._themeXmlDoc.DocumentElement.Attributes["dir"].Value);
      }
    }

    /// <summary>
    ///   Get or Set the current Theme File
    /// </summary>
    public string ThemeFile
    {
      get
      {
        return this._themeFile;
      }

      set
      {
        if (this._themeFile != value)
        {
          if (IsValidTheme(value))
          {
            this._themeFile = value;
            this._themeXmlDoc = null;
          }
        }
      }
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Basic testing of the theme's validity...
    /// </summary>
    /// <param name="themeFile">
    /// </param>
    /// <returns>
    /// The is valid theme.
    /// </returns>
    public static bool IsValidTheme([NotNull] string themeFile)
    {
      CodeContracts.ArgumentNotNull(themeFile, "themeFile");

      if (themeFile.IsNotSet())
      {
        return false;
      }

      themeFile = themeFile.Trim().ToLower();

      if (themeFile.Length == 0)
      {
        return false;
      }

      if (!themeFile.EndsWith(".xml"))
      {
        return false;
      }

      return
        File.Exists(
          HttpContext.Current.Server.MapPath(
            String.Concat(YafForumInfo.ForumServerFileRoot, YafBoardFolders.Current.Themes, "/", themeFile.Trim())));
    }

    /// <summary>
    /// Gets the collapsible panel image url (expanded or collapsed).
    ///   </summary>
    /// <param name="panelID">
    /// ID of collapsible panel
    /// </param>
    /// <param name="defaultState">
    /// Default Panel State
    /// </param>
    /// <returns>
    /// Image URL
    /// </returns>
    public string GetCollapsiblePanelImageURL([NotNull] string panelID, PanelSessionState.CollapsiblePanelState defaultState)
    {
      CodeContracts.ArgumentNotNull(panelID, "panelID");

      PanelSessionState.CollapsiblePanelState stateValue = Mession.PanelState[panelID];
      if (stateValue == PanelSessionState.CollapsiblePanelState.None)
      {
        stateValue = defaultState;
        Mession.PanelState[panelID] = defaultState;
      }

      return this.GetItem(
        "ICONS", stateValue == PanelSessionState.CollapsiblePanelState.Expanded ? "PANEL_COLLAPSE" : "PANEL_EXPAND");
    }

    #endregion

    #region Implemented Interfaces

    #region IYafTheme

    /// <summary>
    /// Gets full path to the given theme file.
    /// </summary>
    /// <param name="filename">
    /// Short name of theme file.
    /// </param>
    /// <returns>
    /// The build theme path.
    /// </returns>
    public string BuildThemePath([NotNull] string filename)
    {
      CodeContracts.ArgumentNotNull(filename, "filename");

      return this.ThemeDir + filename;
    }

    /// <summary>
    /// The get item.
    /// </summary>
    /// <param name="page">
    /// The page.
    /// </param>
    /// <param name="tag">
    /// The tag.
    /// </param>
    /// <returns>
    /// The get item.
    /// </returns>
    public string GetItem([NotNull] string page, [NotNull] string tag)
    {
      CodeContracts.ArgumentNotNull(page, "page");
      CodeContracts.ArgumentNotNull(tag, "tag");

      return this.GetItem(page, tag, "[{0}.{1}]".FormatWith(page.ToUpper(), tag.ToUpper()));
    }

    /// <summary>
    /// The get item.
    /// </summary>
    /// <param name="page">
    /// The page.
    /// </param>
    /// <param name="tag">
    /// The tag.
    /// </param>
    /// <param name="defaultValue">
    /// The default value.
    /// </param>
    /// <returns>
    /// The get item.
    /// </returns>
    public string GetItem([NotNull] string page, [NotNull] string tag, [NotNull] string defaultValue)
    {
      CodeContracts.ArgumentNotNull(page, "page");
      CodeContracts.ArgumentNotNull(tag, "tag");
      CodeContracts.ArgumentNotNull(defaultValue, "defaultValue");

      string item = string.Empty;

      this.LoadThemeFile();

      if (this._themeXmlDoc != null)
      {
        string themeDir = this._themeXmlDoc.DocumentElement.Attributes["dir"].Value;
        string langCode = YafContext.Current.Localization.LanguageCode.ToUpper();
        string select = "//page[@name='{0}']/Resource[@tag='{1}' and @language='{2}']".FormatWith(
          page.ToUpper(), tag.ToUpper(), langCode);

        XmlNode node = this._themeXmlDoc.SelectSingleNode(select);
        if (node == null)
        {
          select = "//page[@name='{0}']/Resource[@tag='{1}']".FormatWith(page.ToUpper(), tag.ToUpper());
          node = this._themeXmlDoc.SelectSingleNode(select);
        }

        if (node == null)
        {
          if (this.LogMissingThemeItem)
          {
            DB.eventlog_create(
              YafContext.Current.PageUserID, 
              page.ToLower() + ".ascx", 
              "Missing Theme Item: {0}.{1}".FormatWith(page.ToUpper(), tag.ToUpper()), 
              EventLogTypes.Error);
          }

          return defaultValue;
        }

        item = node.InnerText.Replace(
          "~", "{0}{1}/{2}".FormatWith(YafForumInfo.ForumServerFileRoot, YafBoardFolders.Current.Themes, themeDir));
      }

      return item;
    }

    #endregion

    #endregion

    #region Methods

    /// <summary>
    /// The load theme file.
    /// </summary>
    private void LoadThemeFile()
    {
      if (this.ThemeFile != null)
      {
#if !DEBUG
        if (_themeXmlDoc == null)
        {
          _themeXmlDoc = (XmlDocument)System.Web.HttpContext.Current.Cache[ThemeFile];
        }
#endif

        if (this._themeXmlDoc == null)
        {
          this._themeXmlDoc = new XmlDocument();
          this._themeXmlDoc.Load(
            HttpContext.Current.Server.MapPath(
              String.Concat(YafForumInfo.ForumServerFileRoot, YafBoardFolders.Current.Themes, "/", this.ThemeFile)));
#if !DEBUG
          System.Web.HttpContext.Current.Cache[ThemeFile] = _themeXmlDoc;
#endif
        }
      }
    }

    #endregion
  }
}