﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Reflection;
using Signum.Web;
using Signum.Utilities;
using Southwind.Entities;
using System.Web.Mvc;
using System.Drawing;
using System.IO;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using Signum.Entities;
using Signum.Entities.Authorization;
using Signum.Engine;
using Signum.Entities.Basics;
using Signum.Entities.SMS;
using Signum.Entities.Mailing;
using Signum.Entities.Files;
using Signum.Web.Files;

namespace Southwind.Web
{
    public static class SouthwindClient
    {
        public static string ViewPrefix = "~/Views/Southwind/{0}.cshtml";
        public static string ThemeSessionKey = "swCurrentTheme";

        public static JsModule OrderModule = new JsModule("Order");

        public static void Start()
        {
            if (Navigator.Manager.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                Navigator.AddSettings(new List<EntitySettings>
                {
                    new EmbeddedEntitySettings<AddressDN>() { PartialViewName = e => ViewPrefix.Formato("Address") },

                    new EntitySettings<TerritoryDN>() { PartialViewName = e => ViewPrefix.Formato("Territory") },
                    new EntitySettings<RegionDN>() { PartialViewName = e => ViewPrefix.Formato("Region") },
                    new EntitySettings<EmployeeDN>() { PartialViewName = e => ViewPrefix.Formato("Employee") },

                    new EntitySettings<SupplierDN>() { PartialViewName = e => ViewPrefix.Formato("Supplier") },
                    new EntitySettings<ProductDN>() { PartialViewName = e => ViewPrefix.Formato("Product") },
                    new EntitySettings<CategoryDN>() { PartialViewName = e => ViewPrefix.Formato("Category") },

                    new EntitySettings<PersonDN>() { PartialViewName = e => ViewPrefix.Formato("Person") },
                    new EntitySettings<CompanyDN>() { PartialViewName = e => ViewPrefix.Formato("Company") },
                   
                    new EntitySettings<OrderDN>() { PartialViewName = e => ViewPrefix.Formato("Order") },
                    new EmbeddedEntitySettings<OrderDetailsDN> { PartialViewName = e => ViewPrefix.Formato("OrderDetails") },
                    new EntitySettings<ShipperDN>() { PartialViewName = e => ViewPrefix.Formato("Shipper") },
                    new EntitySettings<ApplicationConfigurationDN>() { PartialViewName = e => ViewPrefix.Formato("ApplicationConfiguration") },
                });

                Constructor.Register(ctx => new ApplicationConfigurationDN { Sms = new SMSConfigurationDN(), Email = new EmailConfigurationDN() });

                QuerySettings.RegisterPropertyFormat((CategoryDN e) => e.Picture,
                    new CellFormatter((html, obj) => obj == null ? null :
                        new HtmlTag("img")
                       .Attr("src", Base64Data((EmbeddedFileDN)obj))
                      .Attr("alt", obj.ToString())
                      .Attr("style", "width:48px").ToHtmlSelf()) { TextAlign = "center" });

                QuerySettings.RegisterPropertyFormat((EmployeeDN e) => e.Photo,
                    new CellFormatter((html, obj) => obj == null ? null :
                      new HtmlTag("img")
                      .Attr("src", RouteHelper.New().Action((FileController c) => c.Download(new RuntimeInfo((Lite<FileDN>)obj).ToString())))
                      .Attr("alt", obj.ToString())
                      .Attr("style", "width:48px").ToHtmlSelf()) { TextAlign = "center" });

                Constructor.Register(ctx => new EmployeeDN { Address = new AddressDN() });
                Constructor.Register(ctx => new OrderDN
                {
                    ShipAddress = new AddressDN(),
                    Details = new MList<OrderDetailsDN>()

                });
                Constructor.Register(ctx => new PersonDN { Address = new AddressDN() });
                Constructor.Register(ctx => new CompanyDN { Address = new AddressDN() });
                Constructor.Register(ctx => new SupplierDN { Address = new AddressDN() });

                RegisterQuickLinks();
            }
        }

        private static void RegisterQuickLinks()
        {
            LinksClient.RegisterEntityLinks<UserDN>((entity, ctx) => new[]
                {
                    new QuickLinkFind(typeof(OperationLogDN), "User", entity, true)
                });

            LinksClient.RegisterEntityLinks<EmployeeDN>((entity, ctx) =>
            {
                var links = new List<QuickLink>()
                {
                    new QuickLinkFind(typeof(OrderDN), "Employee", entity, true)  
                };

                var user = Database.Query<UserDN>().Where(u => entity.RefersTo(u.Related)).Select(u => u.ToLite()).FirstOrDefault();
                if (user != null)
                    links.Add(new QuickLinkView(user));

                return links.ToArray();
            });

            LinksClient.RegisterEntityLinks<CategoryDN>((entity, ctx) => new[]
            {
                new QuickLinkFind(typeof(ProductDN), "Category", entity, true)
            });

            LinksClient.RegisterEntityLinks<SupplierDN>((entity, ctx) => new[]
            {
                new QuickLinkFind(typeof(ProductDN), "Supplier", entity, true)
            });

            LinksClient.RegisterEntityLinks<PersonDN>((entity, ctx) => new[]
            {
                new QuickLinkFind(typeof(OrderDN), "Customer", entity, true)
            });

            LinksClient.RegisterEntityLinks<CompanyDN>((entity, ctx) => new[]
            {
                new QuickLinkFind(typeof(OrderDN), "Customer", entity, true)
            });
        }

        public static string Base64Data(EmbeddedFileDN file)
        {
            return "data:" + MimeType.FromFileName(file.FileName) + ";base64," + Convert.ToBase64String(file.BinaryFile);
        } //Base64Data
    }
}