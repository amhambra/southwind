﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using Signum.Entities;
using Signum.Entities.Authorization;
using Signum.Utilities;
using Signum.Utilities.Reflection;
using Signum.Windows;
using Signum.Windows.Operations;
using Southwind.Entities;
using Southwind.Windows.Controls;

namespace Southwind.Windows.Code
{
    public static class SouthwindClient
    {
        public static void Start()
        {
            if (Navigator.Manager.NotDefined(MethodInfo.GetCurrentMethod()))
            {

                Navigator.AddSettings(new List<EntitySettings>
                {
                    new EntitySettings<EmployeeDN>() { View = e => new Employee()},
                    new EntitySettings<TerritoryDN>() { View = e => new Territory() },
                    new EntitySettings<RegionDN>() { View = e => new Region() },

                    new EntitySettings<ProductDN>() { View = e => new Product() },
                    new EntitySettings<CategoryDN>() { View = e => new Category() },
                    new EntitySettings<SupplierDN>() { View = e => new Supplier() },

                    new EntitySettings<CompanyDN>() { View = e => new Company() },
                    new EntitySettings<PersonDN>() { View = e => new Person() },

                    new EntitySettings<OrderDN>() { View = e => new Order()},
                });

         
                QuerySettings.RegisterPropertyFormat((EmployeeDN e) => e.Photo, b =>
                {
                    b.Converter = SouthwindConverters.ImageConverter;
                    return Fluent.GetDataTemplate(() => new Image { MaxHeight = 32.0, Stretch = Stretch.Uniform }
                        .Bind(Image.SourceProperty, b)
                        .Set(RenderOptions.BitmapScalingModeProperty, BitmapScalingMode.Linear));
                }); //Photo

                QuerySettings.RegisterPropertyFormat((CategoryDN e) => e.Picture,  b =>
                {
                    b.Converter = SouthwindConverters.EmbeddedImageConverter;
                    return Fluent.GetDataTemplate(() => new Image { MaxHeight = 32.0, Stretch = Stretch.Uniform }
                        .Bind(Image.SourceProperty, b)
                        .Set(RenderOptions.BitmapScalingModeProperty, BitmapScalingMode.Linear));
                }); //Picture

                Constructor.Register(ctx => new EmployeeDN { Address = new AddressDN() });
                Constructor.Register(ctx => new PersonDN { Address = new AddressDN() });
                Constructor.Register(ctx => new CompanyDN { Address = new AddressDN() });
                Constructor.Register(ctx => new SupplierDN { Address = new AddressDN() });

                OperationClient.AddSettings(new List<OperationSettings>()
                {
                    new ConstructorOperationSettings<OrderDN>(OrderOperation.Create)
                    {
                        Constructor = ctx=>
                        {
                            var cust = Finder.Find<CustomerDN>(); // could return null, but we let it continue 

                            return OperationServer.Construct(OrderOperation.Create, cust);
                        },
                    },


                    new ContextualOperationSettings<ProductDN>(OrderOperation.CreateOrderFromProducts)
                    {
                         Click = ctx =>
                         {
                             var cust = Finder.Find<CustomerDN>(); // could return null, but we let it continue 

                             var result = OperationServer.ConstructFromMany(ctx.Entities, OrderOperation.CreateOrderFromProducts, cust);

                             Navigator.Navigate(result);
                         },
                    },

                    new EntityOperationSettings<OrderDN>(OrderOperation.SaveNew){ IsVisible = ctx=> ctx.Entity.IsNew }, 
                    new EntityOperationSettings<OrderDN>(OrderOperation.Save){ IsVisible = ctx=> !ctx.Entity.IsNew }, 

                    new EntityOperationSettings<OrderDN>(OrderOperation.Cancel)
                    { 
                        ConfirmMessage = ctx=> ((OrderDN)ctx.Entity).State == OrderState.Shipped ? OrderMessage.CancelShippedOrder0.NiceToString(ctx.Entity) : null 
                    }, 

                    new EntityOperationSettings<OrderDN>(OrderOperation.Ship)
                    { 
                        Click = ctx =>
                        {
                            if (!ctx.EntityControl.LooseChangesIfAny())
                                return null;

                            DateTime shipDate = DateTime.Now;
                            if (!ValueLineBox.Show(ref shipDate, 
                                labelText: DescriptionManager.NiceName((OrderDN o) => o.ShippedDate), 
                                owner: Window.GetWindow(ctx.EntityControl)))
                                return null;

                            return ctx.Entity.Execute(OrderOperation.Ship, shipDate); 
                        },

                        Contextual = 
                        { 
                            Click = ctx =>
                            {
                                DateTime shipDate = DateTime.Now;
                                if (!ValueLineBox.Show(ref shipDate, 
                                    labelText: DescriptionManager.NiceName((OrderDN o) => o.ShippedDate), 
                                    owner: Window.GetWindow(ctx.SearchControl)))
                                    return;

                                ctx.Entities.SingleEx().ExecuteLite(OrderOperation.Ship, shipDate); 
                            }
                        }
                    }, 
                }); 

                //NotDefined
            }
        }
    }
}
