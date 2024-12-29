/*
 * Licensed to the Apache Software Foundation (ASF) under one
 * or more contributor license agreements.  See the NOTICE file
 * distributed with this work for additional information
 * regarding copyright ownership.  The ASF licenses this file
 * to you under the Apache License, Version 2.0 (the
 * "License"); you may not use this file except in compliance
 * with the License.  You may obtain a copy of the License at
 * 
 *   http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing,
 * software distributed under the License is distributed on an
 * "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
 * KIND, either express or implied.  See the License for the
 * specific language governing permissions and limitations
 * under the License.
 */
using System.Xml.Linq;
using FluentAssertions;


namespace s2industries.ZUGFeRD.Tests
{
    public class ZUGFeRD22Tests : TestBase
    {
        private readonly InvoiceProvider InvoiceProvider = new();

        [Fact]
        public void TestParentLineId()
        {
            var path = @"..\..\..\..\demodata\xRechnung\xRechnung UBL.xml";
            path = _makeSurePathIsCrossPlatformCompatible(path);

            Stream s = File.Open(path, FileMode.Open);
            var desc = InvoiceDescriptor.Load(s);
            s.Close();

            desc.TradeLineItems.Clear();
            desc.AdditionalReferencedDocuments.Clear();

            desc.AddTradeLineItem(
                lineID: "1",
                name: "Trennblätter A4",
                billedQuantity: 20m,
                unitCode: QuantityCodes.H87,
                netUnitPrice: 9.9m,
                grossUnitPrice: 9.9m,
                categoryCode: TaxCategoryCodes.S,
                taxPercent: 19.0m,
                taxType: TaxTypes.VAT);

            desc.AddTradeLineItem(
                lineID: "2",
                name: "Abschlagsrechnungen",
                billedQuantity: 0m,
                unitCode: QuantityCodes.C62,
                netUnitPrice: 0m,
                categoryCode: TaxCategoryCodes.S,
                taxPercent: 0m,
                taxType: TaxTypes.VAT);

            TradeLineItem subTradeLineItem1 = desc.AddTradeLineItem(
                lineID: "2.1",
                name: "Abschlagsrechnung vom 01.01.2024",
                billedQuantity: -1m,
                unitCode: QuantityCodes.C62,
                netUnitPrice: 500,
                categoryCode: TaxCategoryCodes.S,
                taxPercent: 19.0m,
                taxType: TaxTypes.VAT);
            subTradeLineItem1.SetParentLineId("2");

            TradeLineItem subTradeLineItem2 = desc.AddTradeLineItem(
                lineID: "2.2",
                name: "Abschlagsrechnung vom 20.01.2024",
                billedQuantity: -1m,
                unitCode: QuantityCodes.C62,
                netUnitPrice: 500,
                categoryCode: TaxCategoryCodes.S,
                taxPercent: 19.0m,
                taxType: TaxTypes.VAT);
            subTradeLineItem2.SetParentLineId("2");

            TradeLineItem subTradeLineItem3 = desc.AddTradeLineItem(
                lineID: "2.2.1",
                name: "Abschlagsrechnung vom 10.01.2024",
                billedQuantity: -1m,
                unitCode: QuantityCodes.C62,
                netUnitPrice: 100,
                categoryCode: TaxCategoryCodes.S,
                taxPercent: 19.0m,
                taxType: TaxTypes.VAT);
            subTradeLineItem3.SetParentLineId("2.2");

            desc.AddTradeLineItem(
                lineID: "3",
                name: "Joghurt Banane",
                billedQuantity: 50m,
                unitCode: QuantityCodes.H87,
                netUnitPrice: 5.5m,
                grossUnitPrice: 5.5m,
                categoryCode: TaxCategoryCodes.S,
                taxPercent: 7.0m,
                taxType: TaxTypes.VAT);

            var ms = new MemoryStream();

            desc.Save(ms, ZUGFeRDVersion.Version23, Profile.XRechnung, ZUGFeRDFormats.UBL);
            ms.Seek(0, SeekOrigin.Begin);

            var loadedInvoice = InvoiceDescriptor.Load(ms);
            loadedInvoice.TradeLineItems.Should().HaveCount(6);
            loadedInvoice.TradeLineItems.Should().SatisfyRespectively
            (
                first => first.AssociatedDocument.ParentLineID.Should().BeNull(),
                second => second.AssociatedDocument.ParentLineID.Should().BeNull(),
                third => third.AssociatedDocument.ParentLineID.Should().Be("2"),
                fourth => fourth.AssociatedDocument.ParentLineID.Should().Be("2"),
                fifth => fifth.AssociatedDocument.ParentLineID.Should().Be("2.2"),
                sixth => sixth.AssociatedDocument.ParentLineID.Should().BeNull()
            );
        }

        [Fact]
        public void TestLineStatusCode()
        {
            var path = @"..\..\..\..\demodata\zugferd21\zugferd_2p1_EXTENDED_Warenrechnung-factur-x.xml";
            path = _makeSurePathIsCrossPlatformCompatible(path);

            Stream s = File.Open(path, FileMode.Open);
            var desc = InvoiceDescriptor.Load(s);
            s.Close();

            desc.TradeLineItems.Clear();

            TradeLineItem tradeLineItem1 = desc.AddTradeLineItem(
                name: "Trennblätter A4",
                billedQuantity: 20m,
                unitCode: QuantityCodes.H87,
                netUnitPrice: 9.9m,
                grossUnitPrice: 9.9m,
                categoryCode: TaxCategoryCodes.S,
                taxPercent: 19.0m,
                taxType: TaxTypes.VAT);
            tradeLineItem1.SetLineStatus(LineStatusCodes.New, LineStatusReasonCodes.DETAIL);

            desc.AddTradeLineItem(
                name: "Joghurt Banane",
                billedQuantity: 50m,
                unitCode: QuantityCodes.H87,
                netUnitPrice: 5.5m,
                grossUnitPrice: 5.5m,
                categoryCode: TaxCategoryCodes.S,
                taxPercent: 7.0m,
                taxType: TaxTypes.VAT);

            TradeLineItem tradeLineItem3 = desc.AddTradeLineItem(
                name: "Abschlagsrechnung vom 01.01.2024",
                billedQuantity: -1m,
                unitCode: QuantityCodes.C62,
                netUnitPrice: 500,
                categoryCode: TaxCategoryCodes.S,
                taxPercent: 19.0m,
                taxType: TaxTypes.VAT);
            tradeLineItem3.SetLineStatus(LineStatusCodes.DocumentationClaim, LineStatusReasonCodes.INFORMATION);

            var ms = new MemoryStream();

            desc.Save(ms, ZUGFeRDVersion.Version23, Profile.Extended);
            ms.Seek(0, SeekOrigin.Begin);

            var loadedInvoice = InvoiceDescriptor.Load(ms);
            loadedInvoice.TradeLineItems.Should().HaveCount(3);
            loadedInvoice.TradeLineItems.Should().SatisfyRespectively
            (
                first =>
                {
                    first.AssociatedDocument.LineStatusCode.Should().Be(LineStatusCodes.New);
                    first.AssociatedDocument.LineStatusReasonCode.Should().Be(LineStatusReasonCodes.DETAIL);
                },
                second =>
                {
                    second.AssociatedDocument.LineStatusCode.Should().BeNull();
                    second.AssociatedDocument.LineStatusReasonCode.Should().BeNull();
                },
                third =>
                {
                    third.AssociatedDocument.LineStatusCode.Should().Be(LineStatusCodes.DocumentationClaim);
                    third.AssociatedDocument.LineStatusReasonCode.Should().Be(LineStatusReasonCodes.INFORMATION);
                }
            );
        }

        [Fact]
        public void TestExtendedInvoiceWithIncludedItems()
        {
            var path = @"..\..\..\..\demodata\zugferd21\zugferd_2p1_EXTENDED_Warenrechnung-factur-x.xml";
            path = _makeSurePathIsCrossPlatformCompatible(path);

            Stream s = File.Open(path, FileMode.Open);
            var desc = InvoiceDescriptor.Load(s);
            s.Close();

            desc.TradeLineItems.Clear();

            TradeLineItem tradeLineItem = desc.AddTradeLineItem(
                lineID: "1",
                name: "Trennblätter A4",
                billedQuantity: 20m,
                unitCode: QuantityCodes.H87,
                netUnitPrice: 9.9m,
                grossUnitPrice: 9.9m,
                categoryCode: TaxCategoryCodes.S,
                taxPercent: 19.0m,
                taxType: TaxTypes.VAT);

            tradeLineItem.AddIncludedReferencedProduct("Test", 1, QuantityCodes.C62);
            tradeLineItem.AddIncludedReferencedProduct("Test2");

            var ms = new MemoryStream();

            desc.Save(ms, ZUGFeRDVersion.Version23, Profile.Extended);
            ms.Seek(0, SeekOrigin.Begin);

            var loadedInvoice = InvoiceDescriptor.Load(ms);

            loadedInvoice.TradeLineItems.Should().HaveCount(1);
            loadedInvoice.TradeLineItems[0].IncludedReferencedProducts.Should().HaveCount(2);
            loadedInvoice.TradeLineItems[0].IncludedReferencedProducts.Should().SatisfyRespectively
            (
                first =>
                {
                    first.Name.Should().Be("Test");
                    first.UnitQuantity.Should().Be(1);
                    first.UnitCode.Should().Be(QuantityCodes.C62);
                },
                second =>
                {
                    second.Name.Should().Be("Test2");
                    second.UnitQuantity.Should().BeNull();
                    second.UnitCode.Should().BeNull();
                }
            );
        }

        [Fact]
        public void TestReferenceEReportingFacturXInvoice()
        {
            var path = @"..\..\..\..\demodata\zugferd21\zugferd_2p1_EREPORTING-factur-x.xml";
            path = _makeSurePathIsCrossPlatformCompatible(path);

            Stream s = File.Open(path, FileMode.Open);
            var desc = InvoiceDescriptor.Load(s);
            s.Close();

            desc.Profile.Should().Be(Profile.EReporting);
            desc.Type.Should().Be(InvoiceType.Invoice);
            desc.InvoiceNo.Should().Be("471102");
            desc.TradeLineItems.Should().HaveCount(0);
            desc.LineTotalAmount.Should().Be(0.0m);// not present in file
            desc.TaxBasisAmount.Should().Be(198.0m);
            desc.IsTest.Should().BeFalse(); // not present in file
        }

        [Fact]
        public void TestReferenceBasicFacturXInvoice()
        {
            var path = @"..\..\..\..\demodata\zugferd21\zugferd_2p1_BASIC_Einfach-factur-x.xml";
            path = _makeSurePathIsCrossPlatformCompatible(path);

            Stream s = File.Open(path, FileMode.Open);
            var desc = InvoiceDescriptor.Load(s);
            s.Close();

            desc.Profile.Should().Be(Profile.Basic);
            desc.Type.Should().Be(InvoiceType.Invoice);
            desc.InvoiceNo.Should().Be("471102");
            desc.TradeLineItems.Should().HaveCount(1);
            desc.LineTotalAmount.Should().Be(198.0m);
        } // !TestReferenceBasicFacturXInvoice()

        [Fact]
        public void TestStoringReferenceBasicFacturXInvoice()
        {
            var path = @"..\..\..\..\demodata\zugferd21\zugferd_2p1_BASIC_Einfach-factur-x.xml";
            path = _makeSurePathIsCrossPlatformCompatible(path);

            Stream s = File.Open(path, FileMode.Open);
            var originalDesc = InvoiceDescriptor.Load(s);
            s.Close();

            originalDesc.Profile.Should().Be(Profile.Basic);
            originalDesc.Type.Should().Be(InvoiceType.Invoice);
            originalDesc.InvoiceNo.Should().Be("471102");
            originalDesc.TradeLineItems.Should().HaveCount(1);
            originalDesc.TradeLineItems.Should().AllSatisfy(x =>
            {
                x.BillingPeriodStart.Should().BeNull();
                x.BillingPeriodEnd.Should().BeNull();
                x.ApplicableProductCharacteristics.Should().BeEmpty();
            });
            originalDesc.LineTotalAmount.Should().Be(198.0m);
            originalDesc.Taxes[0].TaxAmount.Should().Be(37.62m);
            originalDesc.Taxes[0].Percent.Should().Be(19.0m);
            originalDesc.IsTest = false;

            Stream ms = new MemoryStream();
            originalDesc.Save(ms, ZUGFeRDVersion.Version23, Profile.Basic);
            originalDesc.Save(@"zugferd_2p1_BASIC_Einfach-factur-x_Result.xml", ZUGFeRDVersion.Version23);

            var desc = InvoiceDescriptor.Load(ms);

            desc.Profile.Should().Be(Profile.Basic);
            desc.Type.Should().Be(InvoiceType.Invoice);
            desc.InvoiceNo.Should().Be("471102");
            desc.TradeLineItems.Should().HaveCount(1);
            desc.TradeLineItems.Should().AllSatisfy(x =>
            {
                x.BillingPeriodStart.Should().BeNull();
                x.BillingPeriodEnd.Should().BeNull();
                x.ApplicableProductCharacteristics.Should().BeEmpty();
            });
            desc.LineTotalAmount.Should().Be(198.0m);
            desc.Taxes[0].TaxAmount.Should().Be(37.62m);
            desc.Taxes[0].Percent.Should().Be(19.0m);
        } // !TestStoringReferenceBasicFacturXInvoice()

        [Fact]
        public void TestReferenceBasicWLInvoice()
        {
            var path = @"..\..\..\..\demodata\zugferd21\zugferd_2p1_BASIC-WL_Einfach-factur-x.xml";
            path = _makeSurePathIsCrossPlatformCompatible(path);

            Stream s = File.Open(path, FileMode.Open);
            var desc = InvoiceDescriptor.Load(s);
            s.Close();

            desc.Profile.Should().Be(Profile.BasicWL);
            desc.Type.Should().Be(InvoiceType.Invoice);
            desc.InvoiceNo.Should().Be("471102");
            desc.TradeLineItems.Should().BeEmpty();
            desc.LineTotalAmount.Should().Be(624.90m);
        } // !TestReferenceBasicWLInvoice()

        [Fact]
        public void TestReferenceExtendedInvoice()
        {
            var path = @"..\..\..\..\demodata\zugferd21\zugferd_2p1_EXTENDED_Warenrechnung-factur-x.xml";
            path = _makeSurePathIsCrossPlatformCompatible(path);

            Stream s = File.Open(path, FileMode.Open);
            var desc = InvoiceDescriptor.Load(s);
            s.Close();

            desc.Profile.Should().Be(Profile.Extended);
            desc.Type.Should().Be(InvoiceType.Invoice);
            desc.InvoiceNo.Should().Be("R87654321012345");
            desc.TradeLineItems.Should().HaveCount(6);
            desc.LineTotalAmount.Should().Be(457.20m);

            IList<TradeAllowanceCharge> tradeAllowanceCharges = desc.GetTradeAllowanceCharges();
            tradeAllowanceCharges.Should().HaveCount(4);
            tradeAllowanceCharges.Should().AllSatisfy(x =>
            {
                x.Tax.TypeCode.Should().Be(TaxTypes.VAT);
                x.Tax.CategoryCode.Should().Be(TaxCategoryCodes.S);
            });

            tradeAllowanceCharges.Should().SatisfyRespectively
            (
                first => first.Tax.Percent.Should().Be(19m),
                second => second.Tax.Percent.Should().Be(7m),
                third => third.Tax.Percent.Should().Be(19m),
                fourth => fourth.Tax.Percent.Should().Be(7m)
            );

            desc.ServiceCharges.Should().HaveCount(1);
            desc.ServiceCharges[0].Tax.TypeCode.Should().Be(TaxTypes.VAT);
            desc.ServiceCharges[0].Tax.CategoryCode.Should().Be(TaxCategoryCodes.S);
            desc.ServiceCharges[0].Tax.Percent.Should().Be(19m);
        } // !TestReferenceExtendedInvoice()

        [Fact]
        public void TestReferenceMinimumInvoice()
        {
            var path = @"..\..\..\..\demodata\zugferd21\zugferd_2p1_MINIMUM_Rechnung-factur-x.xml";
            path = _makeSurePathIsCrossPlatformCompatible(path);

            Stream s = File.Open(path, FileMode.Open);
            var desc = InvoiceDescriptor.Load(s);
            s.Close();

            desc.Profile.Should().Be(Profile.Minimum);
            desc.Type.Should().Be(InvoiceType.Invoice);
            desc.InvoiceNo.Should().Be("471102");
            desc.TradeLineItems.Should().BeEmpty();
            desc.LineTotalAmount.Should().Be(0.0m); // not present in file
            desc.TaxBasisAmount.Should().Be(198.0m);
        } // !TestReferenceMinimumInvoice()

        [Fact]
        public void TestReferenceXRechnung1CII()
        {
            var path = @"..\..\..\..\demodata\xRechnung\xRechnung CII.xml";
            path = _makeSurePathIsCrossPlatformCompatible(path);

            var desc = InvoiceDescriptor.Load(path);

            desc.Profile.Should().Be(Profile.XRechnung1);
            desc.Type.Should().Be(InvoiceType.Invoice);
            desc.InvoiceNo.Should().Be("0815-99-1-a");
            desc.TradeLineItems.Should().HaveCount(2);
            desc.LineTotalAmount.Should().Be(1445.98m);
        } // !TestReferenceXRechnung1CII()

        [Fact]
        public void TestElectronicAddress()
        {
            InvoiceDescriptor desc = InvoiceProvider.CreateInvoice();
            desc.SetSellerElectronicAddress("DE123456789", ElectronicAddressSchemeIdentifiers.GermanyVatNumber);
            desc.SetBuyerElectronicAddress("LU987654321", ElectronicAddressSchemeIdentifiers.LuxemburgVatNumber);

            var ms = new MemoryStream();

            desc.Save(ms, ZUGFeRDVersion.Version23, Profile.XRechnung);
            ms.Seek(0, SeekOrigin.Begin);
            desc.SellerElectronicAddress.Address.Should().Be("DE123456789");
            desc.SellerElectronicAddress.ElectronicAddressSchemeID.Should().Be(ElectronicAddressSchemeIdentifiers.GermanyVatNumber);
            desc.BuyerElectronicAddress.Address.Should().Be("LU987654321");
            desc.BuyerElectronicAddress.ElectronicAddressSchemeID.Should().Be(ElectronicAddressSchemeIdentifiers.LuxemburgVatNumber);

            var loadedInvoice = InvoiceDescriptor.Load(ms);
            loadedInvoice.SellerElectronicAddress.Address.Should().Be("DE123456789");
            loadedInvoice.SellerElectronicAddress.ElectronicAddressSchemeID.Should().Be(ElectronicAddressSchemeIdentifiers.GermanyVatNumber);
            loadedInvoice.BuyerElectronicAddress.Address.Should().Be("LU987654321");
            loadedInvoice.BuyerElectronicAddress.ElectronicAddressSchemeID.Should().Be(ElectronicAddressSchemeIdentifiers.LuxemburgVatNumber);
        } // !TestElectronicAddress()

        [Fact]
        public void TestMinimumInvoice()
        {
            InvoiceDescriptor desc = InvoiceProvider.CreateInvoice();
            desc.Invoicee = new Party() // this information will not be stored in the output file since it is available in Extended profile only
            {
                Name = "Invoicee"
            };
            desc.Seller = new Party()
            {
                Name = "Seller",
                SpecifiedLegalOrganization = new LegalOrganization()
                {
                    TradingBusinessName = "Trading business name for seller party"
                }
            };
            desc.TaxBasisAmount = 73; // this information will not be stored in the output file since it is available in Extended profile only
            var ms = new MemoryStream();

            desc.Save(ms, ZUGFeRDVersion.Version23, Profile.Minimum);
            ms.Seek(0, SeekOrigin.Begin);

            var loadedInvoice = InvoiceDescriptor.Load(ms);
            loadedInvoice.Invoicee.Should().BeNull();
            loadedInvoice.Seller.Should().NotBeNull();
            loadedInvoice.Seller.SpecifiedLegalOrganization.Should().NotBeNull();
            loadedInvoice.Seller.SpecifiedLegalOrganization.TradingBusinessName.Should().BeEmpty();
        } // !TestMinimumInvoice()

        [Fact]
        public void TestInvoiceWithAttachmentXRechnung()
        {
            InvoiceDescriptor desc = InvoiceProvider.CreateInvoice();
            var filename = "myrandomdata.bin";
            var data = new byte[32768];
            new Random().NextBytes(data);

            desc.AddAdditionalReferencedDocument(
                id: "My-File",
                typeCode: AdditionalReferencedDocumentTypeCode.ReferenceDocument,
                name: "Ausführbare Datei",
                attachmentBinaryObject: data,
                filename: filename);

            var ms = new MemoryStream();

            desc.Save(ms, ZUGFeRDVersion.Version23, Profile.XRechnung);
            ms.Seek(0, SeekOrigin.Begin);

            var loadedInvoice = InvoiceDescriptor.Load(ms);

            loadedInvoice.AdditionalReferencedDocuments.Should().HaveCount(1);
            foreach (AdditionalReferencedDocument document in loadedInvoice.AdditionalReferencedDocuments)
            {
                if (document.ID == "My-File")
                {
                    document.AttachmentBinaryObject.Should().BeEquivalentTo(data);
                    document.Filename.Should().Be(filename);
                    break;
                }
            }
        } // !TestInvoiceWithAttachmentXRechnung()

        [Fact]
        public void TestInvoiceWithAttachmentExtended()
        {
            InvoiceDescriptor desc = InvoiceProvider.CreateInvoice();
            var filename = "myrandomdata.bin";
            var data = new byte[32768];
            new Random().NextBytes(data);

            desc.AddAdditionalReferencedDocument(
                id: "My-File",
                typeCode: AdditionalReferencedDocumentTypeCode.ReferenceDocument,
                name: "Ausführbare Datei",
                attachmentBinaryObject: data,
                filename: filename);

            var ms = new MemoryStream();

            desc.Save(ms, ZUGFeRDVersion.Version23, Profile.Extended);
            ms.Seek(0, SeekOrigin.Begin);

            var loadedInvoice = InvoiceDescriptor.Load(ms);

            loadedInvoice.AdditionalReferencedDocuments.Should().HaveCount(1);

            foreach (AdditionalReferencedDocument document in loadedInvoice.AdditionalReferencedDocuments)
            {
                if (document.ID == "My-File")
                {
                    document.AttachmentBinaryObject.Should().BeEquivalentTo(data);
                    document.Filename.Should().Be(filename);
                    break;
                }
            }
        } // !TestInvoiceWithAttachmentExtended()


        [Fact]
        public void TestInvoiceWithAttachmentComfort()
        {
            InvoiceDescriptor desc = InvoiceProvider.CreateInvoice();
            var filename = "myrandomdata.bin";
            var data = new byte[32768];
            new Random().NextBytes(data);

            desc.AddAdditionalReferencedDocument(
                id: "My-File",
                typeCode: AdditionalReferencedDocumentTypeCode.ReferenceDocument,
                name: "Ausführbare Datei",
                attachmentBinaryObject: data,
                filename: filename);

            var ms = new MemoryStream();

            desc.Save(ms, ZUGFeRDVersion.Version23, Profile.Comfort);
            ms.Seek(0, SeekOrigin.Begin);

            var loadedInvoice = InvoiceDescriptor.Load(ms);

            loadedInvoice.AdditionalReferencedDocuments.Should().HaveCount(1);

            foreach (AdditionalReferencedDocument document in loadedInvoice.AdditionalReferencedDocuments)
            {
                if (document.ID == "My-File")
                {
                    document.AttachmentBinaryObject.Should().BeEquivalentTo(data);
                    document.Filename.Should().Be(filename);
                    break;
                }
            }
        } // !TestInvoiceWithAttachmentComfort()

        [Fact]
        public void TestInvoiceWithAttachmentBasic()
        {
            InvoiceDescriptor desc = InvoiceProvider.CreateInvoice();
            var filename = "myrandomdata.bin";
            var data = new byte[32768];
            new Random().NextBytes(data);

            desc.AddAdditionalReferencedDocument(
                id: "My-File",
                typeCode: AdditionalReferencedDocumentTypeCode.ReferenceDocument,
                name: "Ausführbare Datei",
                attachmentBinaryObject: data,
                filename: filename);

            var ms = new MemoryStream();

            desc.Save(ms, ZUGFeRDVersion.Version23, Profile.Basic);
            ms.Seek(0, SeekOrigin.Begin);

            var loadedInvoice = InvoiceDescriptor.Load(ms);

            loadedInvoice.AdditionalReferencedDocuments.Should().BeEmpty();
        } // !TestInvoiceWithAttachmentBasic()

        [Fact]
        public void TestXRechnung1()
        {
            InvoiceDescriptor desc = InvoiceProvider.CreateInvoice();

            var ms = new MemoryStream();

            desc.Save(ms, ZUGFeRDVersion.Version23, Profile.XRechnung1);
            ms.Seek(0, SeekOrigin.Begin);
            desc.Profile.Should().Be(Profile.XRechnung1);

            var loadedInvoice = InvoiceDescriptor.Load(ms);
            loadedInvoice.Profile.Should().Be(Profile.XRechnung1);
        } // !TestXRechnung1()

        [Fact]
        public void TestXRechnung2()
        {
            InvoiceDescriptor desc = InvoiceProvider.CreateInvoice();

            var ms = new MemoryStream();

            desc.Save(ms, ZUGFeRDVersion.Version23, Profile.XRechnung);
            desc.Profile.Should().Be(Profile.XRechnung);

            ms.Seek(0, SeekOrigin.Begin);
            var loadedInvoice = InvoiceDescriptor.Load(ms);
            loadedInvoice.Profile.Should().Be(Profile.XRechnung);
        } // !TestXRechnung2()

        [Fact]
        public void TestCreateInvoice_WithProfileEReporting()
        {
            InvoiceDescriptor desc = InvoiceProvider.CreateInvoice();

            var ms = new MemoryStream();

            desc.Save(ms, ZUGFeRDVersion.Version23, Profile.EReporting);
            desc.Profile.Should().Be(Profile.EReporting);

            ms.Seek(0, SeekOrigin.Begin);
            var loadedInvoice = InvoiceDescriptor.Load(ms);
            loadedInvoice.Profile.Should().Be(Profile.EReporting);
        } // !TestCreateInvoice_WithProfileEReporting()

        [Fact]
        public void TestBuyerOrderReferencedDocumentWithExtended()
        {
            var uuid = Guid.NewGuid().ToString();
            DateTime orderDate = DateTime.Today;

            InvoiceDescriptor desc = InvoiceProvider.CreateInvoice();
            desc.SetBuyerOrderReferenceDocument(uuid, orderDate);

            var ms = new MemoryStream();

            desc.Save(ms, ZUGFeRDVersion.Version23, Profile.Extended);
            ms.Seek(0, SeekOrigin.Begin);
            desc.Profile.Should().Be(Profile.Extended);

            var loadedInvoice = InvoiceDescriptor.Load(ms);
            loadedInvoice.OrderNo.Should().Be(uuid);
            loadedInvoice.OrderDate.Should().Be(orderDate);// explicitly not to be set in XRechnung, see separate test case
        } // !TestBuyerOrderReferencedDocumentWithExtended() 

        [Fact]
        public void TestBuyerOrderReferencedDocumentWithXRechnung()
        {
            var uuid = Guid.NewGuid().ToString();
            DateTime orderDate = DateTime.Today;

            InvoiceDescriptor desc = InvoiceProvider.CreateInvoice();
            desc.SetBuyerOrderReferenceDocument(uuid, orderDate);

            var ms = new MemoryStream();

            desc.Save(ms, ZUGFeRDVersion.Version23, Profile.XRechnung);
            ms.Seek(0, SeekOrigin.Begin);
            desc.Profile.Should().Be(Profile.XRechnung);

            var loadedInvoice = InvoiceDescriptor.Load(ms);
            loadedInvoice.OrderNo.Should().Be(uuid);
            loadedInvoice.OrderDate.Should().BeNull(); // explicitly not to be set in XRechnung, see separate test case
        } // !TestBuyerOrderReferencedDocumentWithXRechnung() 

        [Fact]
        public void TestContractReferencedDocumentWithXRechnung()
        {
            var uuid = Guid.NewGuid().ToString();
            DateTime issueDateTime = DateTime.Today;

            InvoiceDescriptor desc = InvoiceProvider.CreateInvoice();
            desc.ContractReferencedDocument = new ContractReferencedDocument()
            {
                ID = uuid,
                IssueDateTime = issueDateTime
            };


            var ms = new MemoryStream();

            desc.Save(ms, ZUGFeRDVersion.Version23, Profile.XRechnung);
            ms.Seek(0, SeekOrigin.Begin);
            desc.Profile.Should().Be(Profile.XRechnung);

            var loadedInvoice = InvoiceDescriptor.Load(ms);
            loadedInvoice.ContractReferencedDocument.ID.Should().Be(uuid);
            loadedInvoice.ContractReferencedDocument.IssueDateTime.Should().BeNull(); // explicitly not to be set in XRechnung
        } // !TestContractReferencedDocumentWithXRechnung()

        [Fact]
        public void TestContractReferencedDocumentWithExtended()
        {
            var uuid = Guid.NewGuid().ToString();
            DateTime issueDateTime = DateTime.Today;

            InvoiceDescriptor desc = InvoiceProvider.CreateInvoice();
            desc.ContractReferencedDocument = new ContractReferencedDocument()
            {
                ID = uuid,
                IssueDateTime = issueDateTime
            };


            var ms = new MemoryStream();

            desc.Save(ms, ZUGFeRDVersion.Version23, Profile.Extended);
            ms.Seek(0, SeekOrigin.Begin);
            desc.Profile.Should().Be(Profile.Extended);

            var loadedInvoice = InvoiceDescriptor.Load(ms);
            loadedInvoice.ContractReferencedDocument.ID.Should().Be(uuid);
            loadedInvoice.ContractReferencedDocument.IssueDateTime.Should().Be(issueDateTime);// explicitly not to be set in XRechnung, see separate test case
        } // !TestContractReferencedDocumentWithExtended()        

        [Fact]
        public void TestTotalRoundingExtended()
        {
            var uuid = Guid.NewGuid().ToString();
            var issueDateTime = DateTime.Today;

            var desc = InvoiceProvider.CreateInvoice();
            desc.ContractReferencedDocument = new ContractReferencedDocument
            {
                ID = uuid,
                IssueDateTime = issueDateTime
            };
            desc.SetTotals(1.99m, 0m, 0m, 0m, 0m, 2m, 0m, 2m, 0.01m);

            var msExtended = new MemoryStream();
            desc.Save(msExtended, ZUGFeRDVersion.Version23, Profile.Extended);
            msExtended.Seek(0, SeekOrigin.Begin);

            var loadedInvoice = InvoiceDescriptor.Load(msExtended);
            loadedInvoice.RoundingAmount.Should().Be(0.01m);

            var msBasic = new MemoryStream();
            desc.Save(msBasic, ZUGFeRDVersion.Version23);
            msBasic.Seek(0, SeekOrigin.Begin);

            loadedInvoice = InvoiceDescriptor.Load(msBasic);
            loadedInvoice.RoundingAmount.Should().Be(0m);
        } // !TestTotalRoundingExtended()

        [Fact]
        public void TestTotalRoundingXRechnung()
        {
            var uuid = Guid.NewGuid().ToString();
            var issueDateTime = DateTime.Today;

            var desc = InvoiceProvider.CreateInvoice();
            desc.ContractReferencedDocument = new ContractReferencedDocument
            {
                ID = uuid,
                IssueDateTime = issueDateTime
            };
            desc.SetTotals(1.99m, 0m, 0m, 0m, 0m, 2m, 0m, 2m, 0.01m);

            var msExtended = new MemoryStream();
            desc.Save(msExtended, ZUGFeRDVersion.Version23, Profile.XRechnung);
            msExtended.Seek(0, SeekOrigin.Begin);

            var loadedInvoice = InvoiceDescriptor.Load(msExtended);
            loadedInvoice.RoundingAmount.Should().Be(0.01m);

            var msBasic = new MemoryStream();
            desc.Save(msBasic, ZUGFeRDVersion.Version23);
            msBasic.Seek(0, SeekOrigin.Begin);

            loadedInvoice = InvoiceDescriptor.Load(msBasic);
            loadedInvoice.RoundingAmount.Should().Be(0m);
        } // !TestTotalRoundingExtended()

        [Fact]
        public void TestMissingPropertiesAreNull()
        {
            var path = @"..\..\..\..\demodata\zugferd21\zugferd_2p1_BASIC_Einfach-factur-x.xml";
            path = _makeSurePathIsCrossPlatformCompatible(path);

            var invoiceDescriptor = InvoiceDescriptor.Load(path);

            invoiceDescriptor.TradeLineItems.Should().AllSatisfy(x =>
            {
                x.BillingPeriodStart.Should().BeNull();
                x.BillingPeriodEnd.Should().BeNull();
            });
        }

        [Fact]
        public void TestMissingPropertiesAreEmpty()
        {
            var path = @"..\..\..\..\demodata\zugferd21\zugferd_2p1_BASIC_Einfach-factur-x.xml";
            path = _makeSurePathIsCrossPlatformCompatible(path);

            var invoiceDescriptor = InvoiceDescriptor.Load(path);

            invoiceDescriptor.TradeLineItems.Should().AllSatisfy(x =>
            {
                x.ApplicableProductCharacteristics.Should().BeEmpty();
            });
        }

        [Fact]
        public void TestReadTradeLineBillingPeriod()
        {
            var path = @"..\..\..\..\demodata\xRechnung\xrechnung with trade line settlement data.xml";
            path = _makeSurePathIsCrossPlatformCompatible(path);

            var invoiceDescriptor = InvoiceDescriptor.Load(path);

            var tradeLineItem = invoiceDescriptor.TradeLineItems.Single();
            tradeLineItem.BillingPeriodStart.Should().Be(new DateTime(2021, 01, 01));
            tradeLineItem.BillingPeriodEnd.Should().Be(new DateTime(2021, 01, 31));
        }

        [Fact]
        public void TestReadTradeLineLineID()
        {
            var path = @"..\..\..\..\demodata\xRechnung\xrechnung with trade line settlement data.xml";
            path = _makeSurePathIsCrossPlatformCompatible(path);

            var invoiceDescriptor = InvoiceDescriptor.Load(path);
            var tradeLineItem = invoiceDescriptor.TradeLineItems.Single();
            tradeLineItem.AssociatedDocument.LineID.Should().Be("2");
        }

        [Fact]
        public void TestReadTradeLineProductCharacteristics()
        {
            var path = @"..\..\..\..\demodata\xRechnung\xrechnung with trade line settlement data.xml";
            path = _makeSurePathIsCrossPlatformCompatible(path);

            var invoiceDescriptor = InvoiceDescriptor.Load(path);
            var tradeLineItem = invoiceDescriptor.TradeLineItems.Single();

            var firstProductCharacteristic = tradeLineItem.ApplicableProductCharacteristics[0];
            firstProductCharacteristic.Description.Should().Be("METER_LOCATION");
            firstProductCharacteristic.Value.Should().Be("DE213410213");

            var secondProductCharacteristic = tradeLineItem.ApplicableProductCharacteristics[1];
            secondProductCharacteristic.Description.Should().Be("METER_NUMBER");
            secondProductCharacteristic.Value.Should().Be("123");
        }

        [Fact]
        public void TestWriteTradeLineProductCharacteristics()
        {
            var path = @"..\..\..\..\demodata\xRechnung\xrechnung with trade line settlement empty.xml";
            path = _makeSurePathIsCrossPlatformCompatible(path);

            var fileStream = File.Open(path, FileMode.Open);
            var originalInvoiceDescriptor = InvoiceDescriptor.Load(fileStream);
            fileStream.Close();

            // Modifiy trade line settlement data
            TradeLineItem item0 = originalInvoiceDescriptor.AddTradeLineItem(name: string.Empty);
            item0.ApplicableProductCharacteristics = new List<ApplicableProductCharacteristic>()
            {
                new ApplicableProductCharacteristic()
                {
                    Description = "Description_1_1",
                    Value = "Value_1_1"
                },
                new ApplicableProductCharacteristic()
                {
                    Description = "Description_1_2",
                    Value = "Value_1_2"
                }
            };

            TradeLineItem item1 = originalInvoiceDescriptor.AddTradeLineItem(name: string.Empty);
            item1.ApplicableProductCharacteristics = new List<ApplicableProductCharacteristic>()
            {
                new ApplicableProductCharacteristic()
                {
                    Description = "Description_2_1",
                    Value = "Value_2_1"
                },
                new ApplicableProductCharacteristic()
                {
                    Description = "Description_2_2",
                    Value = "Value_2_2"
                }
            };

            originalInvoiceDescriptor.IsTest = false;

            using var memoryStream = new MemoryStream();
            originalInvoiceDescriptor.Save(memoryStream, ZUGFeRDVersion.Version23, Profile.Basic);
            originalInvoiceDescriptor.Save(@"xrechnung with trade line settlement filled.xml", ZUGFeRDVersion.Version23);

            // Load Invoice and compare to expected
            var invoiceDescriptor = InvoiceDescriptor.Load(memoryStream);

            var firstTradeLineItem = invoiceDescriptor.TradeLineItems[0];
            firstTradeLineItem.ApplicableProductCharacteristics[0].Description.Should().Be("Description_1_1");
            firstTradeLineItem.ApplicableProductCharacteristics[0].Value.Should().Be("Value_1_1");
            firstTradeLineItem.ApplicableProductCharacteristics[1].Description.Should().Be("Description_1_2");
            firstTradeLineItem.ApplicableProductCharacteristics[1].Value.Should().Be("Value_1_2");

            var secondTradeLineItem = invoiceDescriptor.TradeLineItems[1];
            secondTradeLineItem.ApplicableProductCharacteristics[0].Description.Should().Be("Description_2_1");
            secondTradeLineItem.ApplicableProductCharacteristics[0].Value.Should().Be("Value_2_1");
            secondTradeLineItem.ApplicableProductCharacteristics[1].Description.Should().Be("Description_2_2");
            secondTradeLineItem.ApplicableProductCharacteristics[1].Value.Should().Be("Value_2_2");
        }

        [Fact]
        public void TestWriteTradeLineBillingPeriod()
        {
            // Read XRechnung
            var path = @"..\..\..\..\demodata\xRechnung\xrechnung with trade line settlement empty.xml";
            path = _makeSurePathIsCrossPlatformCompatible(path);

            Stream s = File.Open(path, FileMode.Open);
            var originalInvoiceDescriptor = InvoiceDescriptor.Load(s);
            s.Close();

            // Modifiy trade line settlement data
            originalInvoiceDescriptor.AddTradeLineItem(
                name: string.Empty,
                billingPeriodStart: new DateTime(2020, 1, 1),
                billingPeriodEnd: new DateTime(2021, 1, 1));

            originalInvoiceDescriptor.AddTradeLineItem(
                name: string.Empty,
                billingPeriodStart: new DateTime(2021, 1, 1),
                billingPeriodEnd: new DateTime(2022, 1, 1));

            originalInvoiceDescriptor.IsTest = false;

            using var memoryStream = new MemoryStream();
            originalInvoiceDescriptor.Save(memoryStream, ZUGFeRDVersion.Version23, Profile.Basic);
            originalInvoiceDescriptor.Save(@"xrechnung with trade line settlement filled.xml", ZUGFeRDVersion.Version23);

            // Load Invoice and compare to expected
            var invoiceDescriptor = InvoiceDescriptor.Load(memoryStream);

            var firstTradeLineItem = invoiceDescriptor.TradeLineItems[0];
            firstTradeLineItem.BillingPeriodStart.Should().Be(new DateTime(2020, 1, 1));
            firstTradeLineItem.BillingPeriodEnd.Should().Be(new DateTime(2021, 1, 1));

            var secondTradeLineItem = invoiceDescriptor.TradeLineItems[1];
            secondTradeLineItem.BillingPeriodStart.Should().Be(new DateTime(2021, 1, 1));
            secondTradeLineItem.BillingPeriodEnd.Should().Be(new DateTime(2022, 1, 1));
        }

        [Fact]
        public void TestWriteTradeLineBilledQuantity()
        {
            // Read XRechnung
            var path = @"..\..\..\..\demodata\xRechnung\xrechnung with trade line settlement empty.xml";
            path = _makeSurePathIsCrossPlatformCompatible(path);

            var fileStream = File.Open(path, FileMode.Open);
            var originalInvoiceDescriptor = InvoiceDescriptor.Load(fileStream);
            fileStream.Close();

            // Modifiy trade line settlement data
            originalInvoiceDescriptor.AddTradeLineItem(
                name: string.Empty,
                billedQuantity: 10,
                netUnitPrice: 1);

            originalInvoiceDescriptor.IsTest = false;

            using var memoryStream = new MemoryStream();
            originalInvoiceDescriptor.Save(memoryStream, ZUGFeRDVersion.Version23, Profile.Basic);
            originalInvoiceDescriptor.Save(@"xrechnung with trade line settlement filled.xml", ZUGFeRDVersion.Version23);

            // Load Invoice and compare to expected
            var invoiceDescriptor = InvoiceDescriptor.Load(memoryStream);
            invoiceDescriptor.TradeLineItems[0].BilledQuantity.Should().Be(10);
        }

        [Fact]
        public void TestWriteTradeLineNetUnitPrice()
        {
            // Read XRechnung
            var path = @"..\..\..\..\demodata\xRechnung\xrechnung with trade line settlement empty.xml";
            path = _makeSurePathIsCrossPlatformCompatible(path);

            var fileStream = File.Open(path, FileMode.Open);
            var originalInvoiceDescriptor = InvoiceDescriptor.Load(fileStream);
            fileStream.Close();

            // Modifiy trade line settlement data
            originalInvoiceDescriptor.AddTradeLineItem(name: string.Empty, netUnitPrice: 25);

            originalInvoiceDescriptor.IsTest = false;

            using var memoryStream = new MemoryStream();
            originalInvoiceDescriptor.Save(memoryStream, ZUGFeRDVersion.Version23, Profile.Basic);
            originalInvoiceDescriptor.Save(@"xrechnung with trade line settlement filled.xml", ZUGFeRDVersion.Version23);

            // Load Invoice and compare to expected
            var invoiceDescriptor = InvoiceDescriptor.Load(memoryStream);
            invoiceDescriptor.TradeLineItems[0].NetUnitPrice.Should().Be(25);
        }

        [Fact]
        public void TestWriteTradeLineLineID()
        {
            // Read XRechnung
            var path = @"..\..\..\..\demodata\xRechnung\xrechnung with trade line settlement empty.xml";
            path = _makeSurePathIsCrossPlatformCompatible(path);

            var fileStream = File.Open(path, FileMode.Open);
            var originalInvoiceDescriptor = InvoiceDescriptor.Load(fileStream);
            fileStream.Close();

            // Modifiy trade line settlement data
            originalInvoiceDescriptor.TradeLineItems.RemoveAll(_ => true);

            originalInvoiceDescriptor.AddTradeLineCommentItem(lineID: "2", comment: "Comment_2");
            originalInvoiceDescriptor.AddTradeLineCommentItem(lineID: "3", comment: "Comment_3");
            originalInvoiceDescriptor.IsTest = false;

            using var memoryStream = new MemoryStream();
            originalInvoiceDescriptor.Save(memoryStream, ZUGFeRDVersion.Version23, Profile.Basic);
            originalInvoiceDescriptor.Save(@"xrechnung with trade line settlement filled.xml", ZUGFeRDVersion.Version23);

            // Load Invoice and compare to expected
            var invoiceDescriptor = InvoiceDescriptor.Load(@"xrechnung with trade line settlement filled.xml");

            var firstTradeLineItem = invoiceDescriptor.TradeLineItems[0];
            firstTradeLineItem.AssociatedDocument.LineID.Should().Be("2");

            var secondTradeLineItem = invoiceDescriptor.TradeLineItems[1];
            secondTradeLineItem.AssociatedDocument.LineID.Should().Be("3");
        }

        // BR-DE-13
        [Fact]
        public void TestLoadingSepaPreNotification()
        {
            var path = @"..\..\..\..\demodata\zugferd21\zugferd_2p1_EN16931_SEPA_Prenotification.xml";
            path = _makeSurePathIsCrossPlatformCompatible(path);

            var invoiceDescriptor = InvoiceDescriptor.Load(path);
            invoiceDescriptor.Profile.Should().Be(Profile.Comfort);
            invoiceDescriptor.Type.Should().Be(InvoiceType.Invoice);

            invoiceDescriptor.PaymentMeans.SEPACreditorIdentifier.Should().Be("DE98ZZZ09999999999");
            invoiceDescriptor.PaymentMeans.SEPAMandateReference.Should().Be("REF A-123");
            invoiceDescriptor.DebitorBankAccounts.Should().HaveCount(1);
            invoiceDescriptor.DebitorBankAccounts[0].IBAN.Should().Be("DE21860000000086001055");

            invoiceDescriptor.GetTradePaymentTerms().FirstOrDefault()?
                .Description.Trim()
                .Should()
                .Be("Der Betrag in Höhe von EUR 529,87 wird am 20.03.2018 von Ihrem Konto per SEPA-Lastschrift eingezogen.");
        } // !TestLoadingSepaPreNotification()

        [Fact]
        public void TestStoringSepaPreNotification()
        {
            var d = new InvoiceDescriptor();
            d.Type = InvoiceType.Invoice;
            d.InvoiceNo = "471102";
            d.Currency = CurrencyCodes.EUR;
            d.InvoiceDate = new DateTime(2018, 3, 5);
            d.AddTradeLineItem(
                lineID: "1",
                id: new GlobalID(GlobalIDSchemeIdentifiers.EAN, "4012345001235"),
                sellerAssignedID: "TB100A4",
                name: "Trennblätter A4",
                billedQuantity: 20m,
                unitCode: QuantityCodes.H87,
                netUnitPrice: 9.9m,
                grossUnitPrice: 9.9m,
                categoryCode: TaxCategoryCodes.S,
                taxPercent: 19.0m,
                taxType: TaxTypes.VAT);
            d.AddTradeLineItem(
                lineID: "2",
                id: new GlobalID(GlobalIDSchemeIdentifiers.EAN, "4000050986428"),
                sellerAssignedID: "ARNR2",
                name: "Joghurt Banane",
                billedQuantity: 50m,
                unitCode: QuantityCodes.H87,
                netUnitPrice: 5.5m,
                grossUnitPrice: 5.5m,
                categoryCode: TaxCategoryCodes.S,
                taxPercent: 7.0m,
                taxType: TaxTypes.VAT);
            d.SetSeller(
                id: null,
                globalID: new GlobalID(GlobalIDSchemeIdentifiers.GLN, "4000001123452"),
                name: "Lieferant GmbH",
                postcode: "80333",
                city: "München",
                street: "Lieferantenstraße 20",
                country: CountryCodes.DE,
                legalOrganization: new LegalOrganization(GlobalIDSchemeIdentifiers.GLN, "4000001123452", "Lieferant GmbH"));
            d.SetBuyer(
                id: "GE2020211",
                globalID: new GlobalID(GlobalIDSchemeIdentifiers.GLN, "4000001987658"),
                name: "Kunden AG Mitte",
                postcode: "69876",
                city: "Frankfurt",
                street: "Kundenstraße 15",
                country: CountryCodes.DE);
            d.SetPaymentMeansSepaDirectDebit(
                "DE98ZZZ09999999999",
                "REF A-123");
            d.AddDebitorFinancialAccount(
                "DE21860000000086001055",
                null);
            d.AddTradePaymentTerms(
                "Der Betrag in Höhe von EUR 529,87 wird am 20.03.2018 von Ihrem Konto per SEPA-Lastschrift eingezogen.");
            d.SetTotals(
                473.00m,
                0.00m,
                0.00m,
                473.00m,
                56.87m,
                529.87m,
                0.00m,
                529.87m);
            d.SellerTaxRegistration.Add(
                new TaxRegistration
                {
                    SchemeID = TaxRegistrationSchemeID.FC,
                    No = "201/113/40209"
                });
            d.SellerTaxRegistration.Add(
                new TaxRegistration
                {
                    SchemeID = TaxRegistrationSchemeID.VA,
                    No = "DE123456789"
                });
            d.AddApplicableTradeTax(
                275.00m,
                7.00m,
                TaxTypes.VAT,
                TaxCategoryCodes.S);
            d.AddApplicableTradeTax(
                198.00m,
                19.00m,
                TaxTypes.VAT,
                TaxCategoryCodes.S);

            using var stream = new MemoryStream();
            d.Save(stream, ZUGFeRDVersion.Version23, Profile.Comfort);

            stream.Seek(0, SeekOrigin.Begin);

            var d2 = InvoiceDescriptor.Load(stream);

            d2.PaymentMeans.SEPACreditorIdentifier.Should().Be("DE98ZZZ09999999999");
            d2.PaymentMeans.SEPAMandateReference.Should().Be("REF A-123");
            d2.DebitorBankAccounts.Should().HaveCount(1);
            d2.DebitorBankAccounts[0].IBAN.Should().Be("DE21860000000086001055");
            d.Seller.SpecifiedLegalOrganization.ID.SchemeID.Should().NotBeNull();
            d.Seller.SpecifiedLegalOrganization.ID.SchemeID!.Value.EnumToString().Should().Be("0088");
            d.Seller.SpecifiedLegalOrganization.ID.ID.Should().Be("4000001123452");
            d.Seller.SpecifiedLegalOrganization.TradingBusinessName.Should().Be("Lieferant GmbH");
        } // !TestStoringSepaPreNotification()

        [Fact]
        public void TestValidTaxTypes()
        {
            InvoiceDescriptor invoice = InvoiceProvider.CreateInvoice();
            invoice.TradeLineItems.ForEach(i => i.TaxType = TaxTypes.VAT);

            var ms = new MemoryStream();
            try
            {
                invoice.Save(ms, version: ZUGFeRDVersion.Version23, profile: Profile.Basic);
            }
            catch (UnsupportedException)
            {
                Assert.Fail();
            }

            try
            {
                invoice.Save(ms, version: ZUGFeRDVersion.Version23, profile: Profile.BasicWL);
            }
            catch (UnsupportedException)
            {
                Assert.Fail();
            }

            try
            {
                invoice.Save(ms, version: ZUGFeRDVersion.Version23, profile: Profile.Comfort);
            }
            catch (UnsupportedException)
            {
                Assert.Fail();
            }

            try
            {
                invoice.Save(ms, version: ZUGFeRDVersion.Version23, profile: Profile.Extended);
            }
            catch (UnsupportedException)
            {
                Assert.Fail();
            }

            try
            {
                invoice.Save(ms, version: ZUGFeRDVersion.Version23, profile: Profile.XRechnung1);
            }
            catch (UnsupportedException)
            {
                Assert.Fail();
            }

            invoice.TradeLineItems.ForEach(i => i.TaxType = TaxTypes.AAA);
            try
            {
                invoice.Save(ms, version: ZUGFeRDVersion.Version23, profile: Profile.XRechnung);
            }
            catch (UnsupportedException)
            {
                Assert.Fail();
            }

            // extended profile supports other tax types as well:
            invoice.TradeLineItems.ForEach(i => i.TaxType = TaxTypes.AAA);
            try
            {
                invoice.Save(ms, version: ZUGFeRDVersion.Version23, profile: Profile.Extended);
            }
            catch (UnsupportedException)
            {
                Assert.Fail();
            }
        } // !TestValidTaxTypes()

        [Fact]
        public void TestInvalidTaxTypes()
        {
            InvoiceDescriptor invoice = InvoiceProvider.CreateInvoice();
            invoice.TradeLineItems.ForEach(i => i.TaxType = TaxTypes.AAA);

            var ms = new MemoryStream();
            try
            {
                invoice.Save(ms, version: ZUGFeRDVersion.Version23, profile: Profile.Basic);
            }
            catch (UnsupportedException)
            {
                Assert.Fail();
            }

            try
            {
                invoice.Save(ms, version: ZUGFeRDVersion.Version23, profile: Profile.BasicWL);
            }
            catch (UnsupportedException)
            {
                Assert.Fail();
            }

            try
            {
                invoice.Save(ms, version: ZUGFeRDVersion.Version23, profile: Profile.Comfort);
            }
            catch (UnsupportedException)
            {
                Assert.Fail();
            }

            try
            {
                invoice.Save(ms, version: ZUGFeRDVersion.Version23, profile: Profile.Comfort);
            }
            catch (UnsupportedException)
            {
                Assert.Fail();
            }

            // allowed in extended profile

            try
            {
                invoice.Save(ms, version: ZUGFeRDVersion.Version23, profile: Profile.XRechnung1);
            }
            catch (UnsupportedException)
            {
                Assert.Fail();
            }

            try
            {
                invoice.Save(ms, version: ZUGFeRDVersion.Version23, profile: Profile.XRechnung);
            }
            catch (UnsupportedException)
            {
                Assert.Fail();
            }
        } // !TestInvalidTaxTypes()

        [Fact]
        public void TestAdditionalReferencedDocument()
        {
            var uuid = Guid.NewGuid().ToString();
            DateTime issueDateTime = DateTime.Today;

            InvoiceDescriptor desc = InvoiceProvider.CreateInvoice();
            desc.AddAdditionalReferencedDocument(uuid, AdditionalReferencedDocumentTypeCode.Unknown, issueDateTime, "Additional Test Document");

            var ms = new MemoryStream();
            desc.Save(ms, ZUGFeRDVersion.Version23, Profile.Extended);

            ms.Seek(0, SeekOrigin.Begin);
            var reader = new StreamReader(ms);
            var text = reader.ReadToEnd();

            ms.Seek(0, SeekOrigin.Begin);
            var loadedInvoice = InvoiceDescriptor.Load(ms);
            loadedInvoice.AdditionalReferencedDocuments.Should().HaveCount(1);
            loadedInvoice.AdditionalReferencedDocuments[0].Name.Should().Be("Additional Test Document");
            loadedInvoice.AdditionalReferencedDocuments[0].IssueDateTime.Should().Be(issueDateTime);
        } // !TestAdditionalReferencedDocument()

        [Fact]
        public void TestPartyExtensions()
        {
            InvoiceDescriptor desc = InvoiceProvider.CreateInvoice();
            desc.Invoicee = new Party() // most of this information will NOT be stored in the output file
            {
                Name = "Invoicee",
                ContactName = "Max Mustermann",
                Postcode = "83022",
                City = "Rosenheim",
                Street = "Münchnerstraße 123",
                AddressLine3 = "EG links",
                CountrySubdivisionName = "Bayern",
                Country = CountryCodes.DE
            };

            desc.Payee = new Party() // most of this information will NOT be stored in the output file
            {
                Name = "Payee",
                ContactName = "Max Mustermann",
                Postcode = "83022",
                City = "Rosenheim",
                Street = "Münchnerstraße 123",
                AddressLine3 = "EG links",
                CountrySubdivisionName = "Bayern",
                Country = CountryCodes.DE
            };

            var ms = new MemoryStream();

            // test with Comfort
            desc.Save(ms, ZUGFeRDVersion.Version23, Profile.Comfort);
            ms.Seek(0, SeekOrigin.Begin);

            var loadedInvoice = InvoiceDescriptor.Load(ms);
            loadedInvoice.Invoicee.Should().BeNull();
            loadedInvoice.Seller.Should().NotBeNull();
            loadedInvoice.Payee.Should().NotBeNull();

            loadedInvoice.Seller.Name.Should().Be("Lieferant GmbH");
            loadedInvoice.Seller.Street.Should().Be("Lieferantenstraße 20");
            loadedInvoice.Seller.City.Should().Be("München");
            loadedInvoice.Seller.Postcode.Should().Be("80333");

            loadedInvoice.Payee.Name.Should().Be("Payee");
            loadedInvoice.Payee.ContactName.Should().BeNull();
            loadedInvoice.Payee.Postcode.Should().BeEmpty();
            loadedInvoice.Payee.City.Should().BeEmpty();
            loadedInvoice.Payee.Street.Should().BeEmpty();
            loadedInvoice.Payee.AddressLine3.Should().BeEmpty();
            loadedInvoice.Payee.CountrySubdivisionName.Should().BeEmpty();
            loadedInvoice.Payee.Country.Should().Be(CountryCodes.Unknown);

            // test with Extended
            ms = new MemoryStream();
            desc.Save(ms, ZUGFeRDVersion.Version23, Profile.Extended);
            ms.Seek(0, SeekOrigin.Begin);

            loadedInvoice = InvoiceDescriptor.Load(ms);
            loadedInvoice.Invoicee.Name.Should().Be("Invoicee");
            loadedInvoice.Invoicee.ContactName.Should().Be("Max Mustermann");
            loadedInvoice.Invoicee.Postcode.Should().Be("83022");
            loadedInvoice.Invoicee.City.Should().Be("Rosenheim");
            loadedInvoice.Invoicee.Street.Should().Be("Münchnerstraße 123");
            loadedInvoice.Invoicee.AddressLine3.Should().Be("EG links");
            loadedInvoice.Invoicee.CountrySubdivisionName.Should().Be("Bayern");
            loadedInvoice.Invoicee.Country.Should().Be(CountryCodes.DE);

            loadedInvoice.Seller.Name.Should().Be("Lieferant GmbH");
            loadedInvoice.Seller.Street.Should().Be("Lieferantenstraße 20");
            loadedInvoice.Seller.City.Should().Be("München");
            loadedInvoice.Seller.Postcode.Should().Be("80333");

            loadedInvoice.Payee.Name.Should().Be("Payee");
            loadedInvoice.Payee.ContactName.Should().Be("Max Mustermann");
            loadedInvoice.Payee.Postcode.Should().Be("83022");
            loadedInvoice.Payee.City.Should().Be("Rosenheim");
            loadedInvoice.Payee.Street.Should().Be("Münchnerstraße 123");
            loadedInvoice.Payee.AddressLine3.Should().Be("EG links");
            loadedInvoice.Payee.CountrySubdivisionName.Should().Be("Bayern");
            loadedInvoice.Payee.Country.Should().Be(CountryCodes.DE);

            // 
            // Check the output in the XML for Comfort.
            // REM: In Comfort only ID, GlobalID, Name, and SpecifiedLegalOrganization are allowed.

            desc.Payee = new Party()
            {
                ID = new GlobalID(GlobalIDSchemeIdentifiers.Unknown, "SL1001"),
                Name = "Max Mustermann"
                // Country is not set and should not be written into the XML
            };

            ms = new MemoryStream();
            desc.Save(ms, ZUGFeRDVersion.Version23, Profile.Comfort);
            ms.Seek(0, SeekOrigin.Begin);

            var doc = XDocument.Load(ms);
            XNamespace rsm = "urn:un:unece:uncefact:data:standard:CrossIndustryInvoice:100";
            XNamespace ram = "urn:un:unece:uncefact:data:standard:ReusableAggregateBusinessInformationEntity:100";

            doc.Root
                ?.Element(rsm + "SupplyChainTradeTransaction")
                ?.Element(ram + "ApplicableHeaderTradeSettlement")
                ?.Element(ram + "PayeeTradeParty")
                ?.Element(ram + "ID")?.Value
                .Should().Be("SL1001");
            doc.Root
                ?.Element(rsm + "SupplyChainTradeTransaction")
                ?.Element(ram + "ApplicableHeaderTradeSettlement")
                ?.Element(ram + "PayeeTradeParty")
                ?.Element(ram + "Name")?.Value
                .Should().Be("Max Mustermann");
            doc.Root
                ?.Element(rsm + "SupplyChainTradeTransaction")
                ?.Element(ram + "ApplicableHeaderTradeSettlement")
                ?.Element(ram + "PayeeTradeParty")
                ?.Element(ram + "PostalTradeAddress")?
                .Should().BeNull();// !!!
        } // !TestMinimumInvoice()

        [Theory]
        [InlineData(Profile.Minimum)]
        [InlineData(Profile.Basic)]
        [InlineData(Profile.BasicWL)]
        [InlineData(Profile.Comfort)]
        [InlineData(Profile.Extended)]
        public void TestShipTo(Profile profile)
        {
            Party expected = new()
            {
                ID = new GlobalID(GlobalIDSchemeIdentifiers.Unknown, "SL1001"),
                GlobalID = new GlobalID(GlobalIDSchemeIdentifiers.GLN, "MusterGLN"),
                Name = "AbKunden AG Mitte",
                Postcode = "12345",
                ContactName = "Einheit: 5.OG rechts",
                Street = "Verwaltung Straße 40",
                City = "Musterstadt",
                Country = CountryCodes.DE,
                CountrySubdivisionName = "Hessen",
                AddressLine3 = "",
                Description = ""
            };
            InvoiceDescriptor desc = InvoiceProvider.CreateInvoice();

            desc.ShipTo = expected;

            using var ms = new MemoryStream();
            desc.Save(ms, ZUGFeRDVersion.Version23, profile);
            ms.Seek(0, SeekOrigin.Begin);
            var loadedInvoice = InvoiceDescriptor.Load(ms);

            if (profile == Profile.Minimum)
                loadedInvoice.ShipTo.Should().BeNull();
            else
                loadedInvoice.ShipTo.Should().BeEquivalentTo(expected);
        }

        [Theory]
        [InlineData(Profile.Minimum)]
        [InlineData(Profile.Basic)]
        [InlineData(Profile.BasicWL)]
        [InlineData(Profile.Comfort)]
        [InlineData(Profile.Extended, false)]
        public void TestShipToTradePartyOnItemLevel(Profile profile, bool nullResult = true)
        {
            Party expected = new()
            {
                Name = "ShipTo",
                City = "ShipToCity"
            };
            InvoiceDescriptor desc = InvoiceProvider.CreateInvoice();
            desc.TradeLineItems.First().ShipTo = expected;

            // test minimum
            var ms = new MemoryStream();
            desc.Save(ms, ZUGFeRDVersion.Version23, profile);
            ms.Seek(0, SeekOrigin.Begin);
            var loadedInvoice = InvoiceDescriptor.Load(ms);

            loadedInvoice.TradeLineItems.Should().NotBeEmpty();
            if (nullResult)
                loadedInvoice.TradeLineItems.First().ShipTo.Should().BeNull();
            else
                loadedInvoice.TradeLineItems.First().ShipTo
                    .Should().BeEquivalentTo(expected, options => options.Including(x => x.Name).Including(x => x.City));
        } // !TestShipToTradePartyOnItemLevel()

        //[Fact]
        //public void TestUltimateShipToTradePartyOnItemLevel()
        //{
        //    InvoiceDescriptor desc = InvoiceProvider.CreateInvoice();
        //    desc.TradeLineItems.First().UltimateShipTo = new Party()
        //    {
        //        Name = "ShipTo",
        //        City = "ShipToCity"
        //    };

        //    // test minimum
        //    var ms = new MemoryStream();
        //    desc.Save(ms, ZUGFeRDVersion.Version23, Profile.Minimum);
        //    ms.Seek(0, SeekOrigin.Begin);
        //    var loadedInvoice = InvoiceDescriptor.Load(ms);

        //    Assert.IsNotNull(loadedInvoice.TradeLineItems);
        //    Assert.IsNull(loadedInvoice.TradeLineItems.First().ShipTo);
        //    Assert.IsNull(loadedInvoice.TradeLineItems.First().UltimateShipTo);

        //    // test basic
        //    ms = new MemoryStream();
        //    desc.Save(ms, ZUGFeRDVersion.Version23, Profile.Basic);
        //    ms.Seek(0, SeekOrigin.Begin);
        //    loadedInvoice = InvoiceDescriptor.Load(ms);

        //    Assert.IsNotNull(loadedInvoice.TradeLineItems);
        //    Assert.IsNull(loadedInvoice.TradeLineItems.First().ShipTo);
        //    Assert.IsNull(loadedInvoice.TradeLineItems.First().UltimateShipTo);

        //    // test comfort
        //    ms = new MemoryStream();
        //    desc.Save(ms, ZUGFeRDVersion.Version23, Profile.Comfort);
        //    ms.Seek(0, SeekOrigin.Begin);
        //    loadedInvoice = InvoiceDescriptor.Load(ms);

        //    Assert.IsNotNull(loadedInvoice.TradeLineItems);
        //    Assert.IsNull(loadedInvoice.TradeLineItems.First().ShipTo);
        //    Assert.IsNull(loadedInvoice.TradeLineItems.First().UltimateShipTo);

        //    // test extended
        //    ms = new MemoryStream();
        //    desc.Save(ms, ZUGFeRDVersion.Version23, Profile.Extended);
        //    ms.Seek(0, SeekOrigin.Begin);
        //    loadedInvoice = InvoiceDescriptor.Load(ms);

        //    Assert.IsNotNull(loadedInvoice.TradeLineItems);
        //    Assert.IsNull(loadedInvoice.TradeLineItems.First().ShipTo);
        //    Assert.IsNotNull(loadedInvoice.TradeLineItems.First().UltimateShipTo);

        //    Assert.AreEqual(loadedInvoice.TradeLineItems.First().UltimateShipTo.Name, "ShipTo");
        //    Assert.AreEqual(loadedInvoice.TradeLineItems.First().UltimateShipTo.City, "ShipToCity");
        //} // !TestUltimateShipToTradePartyOnItemLevel()


        //[Fact]
        //public void TestMimetypeOfEmbeddedAttachment()
        //{
        //    InvoiceDescriptor desc = InvoiceProvider.CreateInvoice();
        //    var filename1 = "myrandomdata.pdf";
        //    var filename2 = "myrandomdata.bin";
        //    DateTime timestamp = DateTime.Now.Date;
        //    var data = new byte[32768];
        //    new Random().NextBytes(data);

        //    desc.AddAdditionalReferencedDocument(
        //        id: "My-File-PDF",
        //        typeCode: AdditionalReferencedDocumentTypeCode.ReferenceDocument,
        //        issueDateTime: timestamp,
        //        name: "EmbeddedPdf",
        //        attachmentBinaryObject: data,
        //        filename: filename1);

        //    desc.AddAdditionalReferencedDocument(
        //        id: "My-File-BIN",
        //        typeCode: AdditionalReferencedDocumentTypeCode.ReferenceDocument,
        //        issueDateTime: timestamp.AddDays(-2),
        //        name: "EmbeddedPdf",
        //        attachmentBinaryObject: data,
        //        filename: filename2);

        //    var ms = new MemoryStream();

        //    desc.Save(ms, ZUGFeRDVersion.Version23, Profile.Extended);
        //    ms.Seek(0, SeekOrigin.Begin);

        //    var loadedInvoice = InvoiceDescriptor.Load(ms);

        //    Assert.AreEqual(loadedInvoice.AdditionalReferencedDocuments.Count, 2);

        //    foreach (AdditionalReferencedDocument document in loadedInvoice.AdditionalReferencedDocuments)
        //    {
        //        if (document.ID == "My-File-PDF")
        //        {
        //            Assert.AreEqual(document.Filename, filename1);
        //            Assert.AreEqual("application/pdf", document.MimeType);
        //            Assert.AreEqual(timestamp, document.IssueDateTime);
        //        }

        //        if (document.ID == "My-File-BIN")
        //        {
        //            Assert.AreEqual(document.Filename, filename2);
        //            Assert.AreEqual("application/octet-stream", document.MimeType);
        //            Assert.AreEqual(timestamp.AddDays(-2), document.IssueDateTime);
        //        }
        //    }
        //} // !TestMimetypeOfEmbeddedAttachment()

        //[Fact]
        //public void TestOrderInformation()
        //{
        //    var path = @"..\..\..\..\demodata\zugferd21\zugferd_2p1_EXTENDED_Warenrechnung-factur-x.xml";
        //    path = _makeSurePathIsCrossPlatformCompatible(path);

        //    DateTime timestamp = DateTime.Now.Date;

        //    Stream s = File.Open(path, FileMode.Open);
        //    var desc = InvoiceDescriptor.Load(s);
        //    desc.OrderDate = timestamp;
        //    desc.OrderNo = "12345";
        //    s.Close();

        //    var ms = new MemoryStream();
        //    desc.Save(ms, ZUGFeRDVersion.Version23, Profile.Extended);

        //    desc.Save("..\\output.xml", ZUGFeRDVersion.Version23, Profile.Extended);

        //    ms.Seek(0, SeekOrigin.Begin);
        //    var reader = new StreamReader(ms);
        //    var text = reader.ReadToEnd();

        //    ms.Seek(0, SeekOrigin.Begin);
        //    var loadedInvoice = InvoiceDescriptor.Load(ms);
        //    Assert.AreEqual(timestamp, loadedInvoice.OrderDate);
        //    Assert.AreEqual("12345", loadedInvoice.OrderNo);

        //} // !TestOrderInformation()

        //[Fact]
        //public void TestSellerOrderReferencedDocument()
        //{
        //    var uuid = Guid.NewGuid().ToString();
        //    DateTime issueDateTime = DateTime.Today;

        //    InvoiceDescriptor desc = InvoiceProvider.CreateInvoice();
        //    desc.SellerOrderReferencedDocument = new SellerOrderReferencedDocument()
        //    {
        //        ID = uuid,
        //        IssueDateTime = issueDateTime
        //    };

        //    var ms = new MemoryStream();
        //    desc.Save(ms, ZUGFeRDVersion.Version23, Profile.Extended);

        //    ms.Seek(0, SeekOrigin.Begin);
        //    var reader = new StreamReader(ms);
        //    var text = reader.ReadToEnd();

        //    ms.Seek(0, SeekOrigin.Begin);
        //    var loadedInvoice = InvoiceDescriptor.Load(ms);

        //    Assert.AreEqual(Profile.Extended, loadedInvoice.Profile);
        //    Assert.AreEqual(uuid, loadedInvoice.SellerOrderReferencedDocument.ID);
        //    Assert.AreEqual(issueDateTime, loadedInvoice.SellerOrderReferencedDocument.IssueDateTime);
        //} // !TestSellerOrderReferencedDocument()

        //[Fact]
        //public void TestWriteAndReadBusinessProcess()
        //{
        //    InvoiceDescriptor desc = InvoiceProvider.CreateInvoice();
        //    desc.BusinessProcess = "A1";

        //    var ms = new MemoryStream();
        //    desc.Save(ms, ZUGFeRDVersion.Version23, Profile.Extended);
        //    ms.Seek(0, SeekOrigin.Begin);
        //    var loadedInvoice = InvoiceDescriptor.Load(ms);

        //    Assert.AreEqual("A1", loadedInvoice.BusinessProcess);
        //} // !TestWriteAndReadBusinessProcess

        ///// <summary>
        ///// This test ensure that Writer and Reader uses the same path and namespace for elements
        ///// </summary>
        //[Fact]
        //public void TestWriteAndReadExtended()
        //{
        //    InvoiceDescriptor desc = InvoiceProvider.CreateInvoice();
        //    var filename2 = "myrandomdata.bin";
        //    DateTime timestamp = DateTime.Now.Date;
        //    var data = new byte[32768];
        //    new Random().NextBytes(data);

        //    desc.AddAdditionalReferencedDocument(
        //        id: "My-File-BIN",
        //        typeCode: AdditionalReferencedDocumentTypeCode.ReferenceDocument,
        //        issueDateTime: timestamp.AddDays(-2),
        //        name: "EmbeddedPdf",
        //        attachmentBinaryObject: data,
        //        filename: filename2);

        //    desc.OrderNo = "12345";
        //    desc.OrderDate = timestamp;

        //    desc.SetContractReferencedDocument("12345", timestamp);

        //    desc.SpecifiedProcuringProject = new SpecifiedProcuringProject { ID = "123", Name = "Project 123" };

        //    desc.ShipTo = new Party
        //    {
        //        ID = new GlobalID(GlobalIDSchemeIdentifiers.Unknown, "123"),
        //        GlobalID = new GlobalID(GlobalIDSchemeIdentifiers.DUNS, "789"),
        //        Name = "Ship To",
        //        ContactName = "Max Mustermann",
        //        Street = "Münchnerstr. 55",
        //        Postcode = "83022",
        //        City = "Rosenheim",
        //        Country = CountryCodes.DE
        //    };

        //    desc.UltimateShipTo = new Party
        //    {
        //        ID = new GlobalID(GlobalIDSchemeIdentifiers.Unknown, "123"),
        //        GlobalID = new GlobalID(GlobalIDSchemeIdentifiers.DUNS, "789"),
        //        Name = "Ultimate Ship To",
        //        ContactName = "Max Mustermann",
        //        Street = "Münchnerstr. 55",
        //        Postcode = "83022",
        //        City = "Rosenheim",
        //        Country = CountryCodes.DE
        //    };

        //    desc.ShipFrom = new Party
        //    {
        //        ID = new GlobalID(GlobalIDSchemeIdentifiers.Unknown, "123"),
        //        GlobalID = new GlobalID(GlobalIDSchemeIdentifiers.DUNS, "789"),
        //        Name = "Ship From",
        //        ContactName = "Eva Musterfrau",
        //        Street = "Alpenweg 5",
        //        Postcode = "83022",
        //        City = "Rosenheim",
        //        Country = CountryCodes.DE
        //    };

        //    desc.PaymentMeans.SEPACreditorIdentifier = "SepaID";
        //    desc.PaymentMeans.SEPAMandateReference = "SepaMandat";
        //    desc.PaymentMeans.FinancialCard = new FinancialCard { Id = "123", CardholderName = "Mustermann" };

        //    desc.PaymentReference = "PaymentReference";

        //    desc.Invoicee = new Party()
        //    {
        //        Name = "Test",
        //        ContactName = "Max Mustermann",
        //        Postcode = "83022",
        //        City = "Rosenheim",
        //        Street = "Münchnerstraße 123",
        //        AddressLine3 = "EG links",
        //        CountrySubdivisionName = "Bayern",
        //        Country = CountryCodes.DE
        //    };

        //    desc.Payee = new Party() // this information will not be stored in the output file since it is available in Extended profile only
        //    {
        //        Name = "Test",
        //        ContactName = "Max Mustermann",
        //        Postcode = "83022",
        //        City = "Rosenheim",
        //        Street = "Münchnerstraße 123",
        //        AddressLine3 = "EG links",
        //        CountrySubdivisionName = "Bayern",
        //        Country = CountryCodes.DE
        //    };

        //    desc.AddDebitorFinancialAccount(iban: "DE02120300000000202052", bic: "BYLADEM1001", bankName: "Musterbank");
        //    desc.BillingPeriodStart = timestamp;
        //    desc.BillingPeriodEnd = timestamp.AddDays(14);

        //    desc.AddTradeAllowanceCharge(false, 5m, CurrencyCodes.EUR, 15m, "Reason for charge", TaxTypes.AAB, TaxCategoryCodes.AB, 19m);
        //    desc.AddLogisticsServiceCharge(10m, "Logistics service charge", TaxTypes.AAC, TaxCategoryCodes.AC, 7m);

        //    desc.GetTradePaymentTerms().FirstOrDefault().DueDate = timestamp.AddDays(14);
        //    desc.AddInvoiceReferencedDocument("RE-12345", timestamp);


        //    //set additional LineItem data
        //    var lineItem = desc.TradeLineItems.FirstOrDefault(i => i.SellerAssignedID == "TB100A4");
        //    Assert.IsNotNull(lineItem);
        //    lineItem.Description = "This is line item TB100A4";
        //    lineItem.BuyerAssignedID = "0815";
        //    lineItem.SetOrderReferencedDocument("12345", timestamp);
        //    lineItem.SetDeliveryNoteReferencedDocument("12345", timestamp);
        //    lineItem.SetContractReferencedDocument("12345", timestamp);

        //    lineItem.AddAdditionalReferencedDocument("xyz", AdditionalReferencedDocumentTypeCode.ReferenceDocument, ReferenceTypeCodes.AAB, timestamp);

        //    lineItem.UnitQuantity = 3m;
        //    lineItem.ActualDeliveryDate = timestamp;

        //    lineItem.ApplicableProductCharacteristics.Add(new ApplicableProductCharacteristic
        //    {
        //        Description = "Product characteristics",
        //        Value = "Product value"
        //    });

        //    lineItem.BillingPeriodStart = timestamp;
        //    lineItem.BillingPeriodEnd = timestamp.AddDays(10);

        //    lineItem.AddReceivableSpecifiedTradeAccountingAccount("987654");
        //    lineItem.AddTradeAllowanceCharge(false, CurrencyCodes.EUR, 10m, 50m, "Reason: UnitTest");


        //    var ms = new MemoryStream();
        //    desc.Save(ms, ZUGFeRDVersion.Version23, Profile.Extended);

        //    ms.Seek(0, SeekOrigin.Begin);
        //    Assert.AreEqual(InvoiceDescriptor.GetVersion(ms), ZUGFeRDVersion.Version23);

        //    ms.Seek(0, SeekOrigin.Begin);
        //    var loadedInvoice = InvoiceDescriptor.Load(ms);

        //    Assert.AreEqual("471102", loadedInvoice.InvoiceNo);
        //    Assert.AreEqual(new DateTime(2018, 03, 05), loadedInvoice.InvoiceDate);
        //    Assert.AreEqual(CurrencyCodes.EUR, loadedInvoice.Currency);
        //    Assert.IsTrue(loadedInvoice.Notes.Any(n => n.Content == "Rechnung gemäß Bestellung vom 01.03.2018."));
        //    Assert.AreEqual("04011000-12345-34", loadedInvoice.ReferenceOrderNo);
        //    Assert.AreEqual("Lieferant GmbH", loadedInvoice.Seller.Name);
        //    Assert.AreEqual("80333", loadedInvoice.Seller.Postcode);
        //    Assert.AreEqual("München", loadedInvoice.Seller.City);

        //    Assert.AreEqual("Lieferantenstraße 20", loadedInvoice.Seller.Street);
        //    Assert.AreEqual(CountryCodes.DE, loadedInvoice.Seller.Country);
        //    Assert.AreEqual(GlobalIDSchemeIdentifiers.GLN, loadedInvoice.Seller.GlobalID.SchemeID);
        //    Assert.AreEqual("4000001123452", loadedInvoice.Seller.GlobalID.ID);
        //    Assert.AreEqual("Max Mustermann", loadedInvoice.SellerContact.Name);
        //    Assert.AreEqual("Muster-Einkauf", loadedInvoice.SellerContact.OrgUnit);
        //    Assert.AreEqual("Max@Mustermann.de", loadedInvoice.SellerContact.EmailAddress);
        //    Assert.AreEqual("+49891234567", loadedInvoice.SellerContact.PhoneNo);

        //    Assert.AreEqual("Kunden AG Mitte", loadedInvoice.Buyer.Name);
        //    Assert.AreEqual("69876", loadedInvoice.Buyer.Postcode);
        //    Assert.AreEqual("Frankfurt", loadedInvoice.Buyer.City);
        //    Assert.AreEqual("Kundenstraße 15", loadedInvoice.Buyer.Street);
        //    Assert.AreEqual(CountryCodes.DE, loadedInvoice.Buyer.Country);
        //    Assert.AreEqual<string>("GE2020211", loadedInvoice.Buyer.ID.ID);

        //    Assert.AreEqual("12345", loadedInvoice.OrderNo);
        //    Assert.AreEqual(timestamp, loadedInvoice.OrderDate);

        //    Assert.AreEqual("12345", loadedInvoice.ContractReferencedDocument.ID);
        //    Assert.AreEqual(timestamp, loadedInvoice.ContractReferencedDocument.IssueDateTime);

        //    Assert.AreEqual("123", loadedInvoice.SpecifiedProcuringProject.ID);
        //    Assert.AreEqual("Project 123", loadedInvoice.SpecifiedProcuringProject.Name);

        //    Assert.AreEqual("Ultimate Ship To", loadedInvoice.UltimateShipTo.Name);
        //    /** 
        //     * @todo we can add further asserts for the remainder of properties 
        //     */

        //    Assert.AreEqual<string>("123", loadedInvoice.ShipTo.ID.ID);
        //    Assert.AreEqual(GlobalIDSchemeIdentifiers.DUNS, loadedInvoice.ShipTo.GlobalID.SchemeID);
        //    Assert.AreEqual("789", loadedInvoice.ShipTo.GlobalID.ID);
        //    Assert.AreEqual("Ship To", loadedInvoice.ShipTo.Name);
        //    Assert.AreEqual("Max Mustermann", loadedInvoice.ShipTo.ContactName);
        //    Assert.AreEqual("Münchnerstr. 55", loadedInvoice.ShipTo.Street);
        //    Assert.AreEqual("83022", loadedInvoice.ShipTo.Postcode);
        //    Assert.AreEqual("Rosenheim", loadedInvoice.ShipTo.City);
        //    Assert.AreEqual(CountryCodes.DE, loadedInvoice.ShipTo.Country);

        //    Assert.AreEqual<string>("123", loadedInvoice.ShipFrom.ID.ID);
        //    Assert.AreEqual(GlobalIDSchemeIdentifiers.DUNS, loadedInvoice.ShipFrom.GlobalID.SchemeID);
        //    Assert.AreEqual("789", loadedInvoice.ShipFrom.GlobalID.ID);
        //    Assert.AreEqual("Ship From", loadedInvoice.ShipFrom.Name);
        //    Assert.AreEqual("Eva Musterfrau", loadedInvoice.ShipFrom.ContactName);
        //    Assert.AreEqual("Alpenweg 5", loadedInvoice.ShipFrom.Street);
        //    Assert.AreEqual("83022", loadedInvoice.ShipFrom.Postcode);
        //    Assert.AreEqual("Rosenheim", loadedInvoice.ShipFrom.City);
        //    Assert.AreEqual(CountryCodes.DE, loadedInvoice.ShipFrom.Country);


        //    Assert.AreEqual(new DateTime(2018, 03, 05), loadedInvoice.ActualDeliveryDate);
        //    Assert.AreEqual(PaymentMeansTypeCodes.SEPACreditTransfer, loadedInvoice.PaymentMeans.TypeCode);
        //    Assert.AreEqual("Zahlung per SEPA Überweisung.", loadedInvoice.PaymentMeans.Information);

        //    Assert.AreEqual("PaymentReference", loadedInvoice.PaymentReference);

        //    Assert.AreEqual("", loadedInvoice.PaymentMeans.SEPACreditorIdentifier);
        //    Assert.AreEqual("SepaMandat", loadedInvoice.PaymentMeans.SEPAMandateReference);
        //    Assert.AreEqual("123", loadedInvoice.PaymentMeans.FinancialCard.Id);
        //    Assert.AreEqual("Mustermann", loadedInvoice.PaymentMeans.FinancialCard.CardholderName);

        //    var bankAccount = loadedInvoice.CreditorBankAccounts.FirstOrDefault(a => a.IBAN == "DE02120300000000202051");
        //    Assert.IsNotNull(bankAccount);
        //    Assert.AreEqual("Kunden AG", bankAccount.Name);
        //    Assert.AreEqual("DE02120300000000202051", bankAccount.IBAN);
        //    Assert.AreEqual("BYLADEM1001", bankAccount.BIC);

        //    var debitorBankAccount = loadedInvoice.DebitorBankAccounts.FirstOrDefault(a => a.IBAN == "DE02120300000000202052");
        //    Assert.IsNotNull(debitorBankAccount);
        //    Assert.AreEqual("DE02120300000000202052", debitorBankAccount.IBAN);


        //    Assert.AreEqual("Test", loadedInvoice.Invoicee.Name);
        //    Assert.AreEqual("Max Mustermann", loadedInvoice.Invoicee.ContactName);
        //    Assert.AreEqual("83022", loadedInvoice.Invoicee.Postcode);
        //    Assert.AreEqual("Rosenheim", loadedInvoice.Invoicee.City);
        //    Assert.AreEqual("Münchnerstraße 123", loadedInvoice.Invoicee.Street);
        //    Assert.AreEqual("EG links", loadedInvoice.Invoicee.AddressLine3);
        //    Assert.AreEqual("Bayern", loadedInvoice.Invoicee.CountrySubdivisionName);
        //    Assert.AreEqual(CountryCodes.DE, loadedInvoice.Invoicee.Country);

        //    Assert.AreEqual("Test", loadedInvoice.Payee.Name);
        //    Assert.AreEqual("Max Mustermann", loadedInvoice.Payee.ContactName);
        //    Assert.AreEqual("83022", loadedInvoice.Payee.Postcode);
        //    Assert.AreEqual("Rosenheim", loadedInvoice.Payee.City);
        //    Assert.AreEqual("Münchnerstraße 123", loadedInvoice.Payee.Street);
        //    Assert.AreEqual("EG links", loadedInvoice.Payee.AddressLine3);
        //    Assert.AreEqual("Bayern", loadedInvoice.Payee.CountrySubdivisionName);
        //    Assert.AreEqual(CountryCodes.DE, loadedInvoice.Payee.Country);


        //    var tax = loadedInvoice.Taxes.FirstOrDefault(t => t.BasisAmount == 275m);
        //    Assert.IsNotNull(tax);
        //    Assert.AreEqual(275m, tax.BasisAmount);
        //    Assert.AreEqual(7m, tax.Percent);
        //    Assert.AreEqual(TaxTypes.VAT, tax.TypeCode);
        //    Assert.AreEqual(TaxCategoryCodes.S, tax.CategoryCode);

        //    Assert.AreEqual(timestamp, loadedInvoice.BillingPeriodStart);
        //    Assert.AreEqual(timestamp.AddDays(14), loadedInvoice.BillingPeriodEnd);

        //    //TradeAllowanceCharges
        //    var tradeAllowanceCharge = loadedInvoice.GetTradeAllowanceCharges().FirstOrDefault(i => i.Reason == "Reason for charge");
        //    Assert.IsNotNull(tradeAllowanceCharge);
        //    Assert.IsTrue(tradeAllowanceCharge.ChargeIndicator);
        //    Assert.AreEqual("Reason for charge", tradeAllowanceCharge.Reason);
        //    Assert.AreEqual(5m, tradeAllowanceCharge.BasisAmount);
        //    Assert.AreEqual(15m, tradeAllowanceCharge.ActualAmount);
        //    Assert.AreEqual(CurrencyCodes.EUR, tradeAllowanceCharge.Currency);
        //    Assert.AreEqual(19m, tradeAllowanceCharge.Tax.Percent);
        //    Assert.AreEqual(TaxTypes.AAB, tradeAllowanceCharge.Tax.TypeCode);
        //    Assert.AreEqual(TaxCategoryCodes.AB, tradeAllowanceCharge.Tax.CategoryCode);

        //    //ServiceCharges
        //    var serviceCharge = desc.ServiceCharges.FirstOrDefault(i => i.Description == "Logistics service charge");
        //    Assert.IsNotNull(serviceCharge);
        //    Assert.AreEqual("Logistics service charge", serviceCharge.Description);
        //    Assert.AreEqual(10m, serviceCharge.Amount);
        //    Assert.AreEqual(7m, serviceCharge.Tax.Percent);
        //    Assert.AreEqual(TaxTypes.AAC, serviceCharge.Tax.TypeCode);
        //    Assert.AreEqual(TaxCategoryCodes.AC, serviceCharge.Tax.CategoryCode);

        //    //PaymentTerms
        //    var paymentTerms = loadedInvoice.GetTradePaymentTerms().FirstOrDefault();
        //    Assert.IsNotNull(paymentTerms);
        //    Assert.AreEqual("Zahlbar innerhalb 30 Tagen netto bis 04.04.2018, 3% Skonto innerhalb 10 Tagen bis 15.03.2018", paymentTerms.Description);
        //    Assert.AreEqual(timestamp.AddDays(14), paymentTerms.DueDate);

        //    Assert.AreEqual(473.0m, loadedInvoice.LineTotalAmount);
        //    Assert.AreEqual(0m, loadedInvoice.ChargeTotalAmount); // mandatory, even if 0!
        //    Assert.AreEqual(0m, loadedInvoice.AllowanceTotalAmount); // mandatory, even if 0!
        //    Assert.AreEqual(473.0m, loadedInvoice.TaxBasisAmount);
        //    Assert.AreEqual(56.87m, loadedInvoice.TaxTotalAmount);
        //    Assert.AreEqual(529.87m, loadedInvoice.GrandTotalAmount);
        //    Assert.AreEqual(null, loadedInvoice.TotalPrepaidAmount); // optional
        //    Assert.AreEqual(529.87m, loadedInvoice.DuePayableAmount);

        //    //InvoiceReferencedDocument
        //    Assert.AreEqual("RE-12345", loadedInvoice.GetInvoiceReferencedDocuments().First().ID);
        //    Assert.AreEqual(timestamp, loadedInvoice.GetInvoiceReferencedDocuments().First().IssueDateTime);


        //    //Line items
        //    var loadedLineItem = loadedInvoice.TradeLineItems.FirstOrDefault(i => i.SellerAssignedID == "TB100A4");
        //    Assert.IsNotNull(loadedLineItem);
        //    Assert.IsTrue(!string.IsNullOrEmpty(loadedLineItem.AssociatedDocument.LineID));
        //    Assert.AreEqual("This is line item TB100A4", loadedLineItem.Description);

        //    Assert.AreEqual("Trennblätter A4", loadedLineItem.Name);

        //    Assert.AreEqual("TB100A4", loadedLineItem.SellerAssignedID);
        //    Assert.AreEqual("0815", loadedLineItem.BuyerAssignedID);
        //    Assert.AreEqual(GlobalIDSchemeIdentifiers.EAN, loadedLineItem.GlobalID.SchemeID);
        //    Assert.AreEqual("4012345001235", loadedLineItem.GlobalID.ID);

        //    //GrossPriceProductTradePrice
        //    Assert.AreEqual(9.9m, loadedLineItem.GrossUnitPrice);
        //    Assert.AreEqual(QuantityCodes.H87, loadedLineItem.UnitCode);
        //    Assert.AreEqual(3m, loadedLineItem.UnitQuantity);

        //    //NetPriceProductTradePrice
        //    Assert.AreEqual(9.9m, loadedLineItem.NetUnitPrice);
        //    Assert.AreEqual(20m, loadedLineItem.BilledQuantity);

        //    Assert.AreEqual(TaxTypes.VAT, loadedLineItem.TaxType);
        //    Assert.AreEqual(TaxCategoryCodes.S, loadedLineItem.TaxCategoryCode);
        //    Assert.AreEqual(19m, loadedLineItem.TaxPercent);

        //    Assert.AreEqual("12345", loadedLineItem.BuyerOrderReferencedDocument.ID);
        //    Assert.AreEqual(timestamp, loadedLineItem.BuyerOrderReferencedDocument.IssueDateTime);
        //    Assert.AreEqual("12345", loadedLineItem.DeliveryNoteReferencedDocument.ID);
        //    Assert.AreEqual(timestamp, loadedLineItem.DeliveryNoteReferencedDocument.IssueDateTime);
        //    Assert.AreEqual("12345", loadedLineItem.ContractReferencedDocument.ID);
        //    Assert.AreEqual(timestamp, loadedLineItem.ContractReferencedDocument.IssueDateTime);

        //    var lineItemReferencedDoc = loadedLineItem.GetAdditionalReferencedDocuments().FirstOrDefault();
        //    Assert.IsNotNull(lineItemReferencedDoc);
        //    Assert.AreEqual("xyz", lineItemReferencedDoc.ID);
        //    Assert.AreEqual(AdditionalReferencedDocumentTypeCode.ReferenceDocument, lineItemReferencedDoc.TypeCode);
        //    Assert.AreEqual(timestamp, lineItemReferencedDoc.IssueDateTime);
        //    Assert.AreEqual(ReferenceTypeCodes.AAB, lineItemReferencedDoc.ReferenceTypeCode);


        //    var productCharacteristics = loadedLineItem.ApplicableProductCharacteristics.FirstOrDefault();
        //    Assert.IsNotNull(productCharacteristics);
        //    Assert.AreEqual("Product characteristics", productCharacteristics.Description);
        //    Assert.AreEqual("Product value", productCharacteristics.Value);

        //    Assert.AreEqual(timestamp, loadedLineItem.ActualDeliveryDate);
        //    Assert.AreEqual(timestamp, loadedLineItem.BillingPeriodStart);
        //    Assert.AreEqual(timestamp.AddDays(10), loadedLineItem.BillingPeriodEnd);

        //    var accountingAccount = loadedLineItem.ReceivableSpecifiedTradeAccountingAccounts.FirstOrDefault();
        //    Assert.IsNotNull(accountingAccount);
        //    Assert.AreEqual("987654", accountingAccount.TradeAccountID);


        //    var lineItemTradeAllowanceCharge = loadedLineItem.GetTradeAllowanceCharges().FirstOrDefault(i => i.Reason == "Reason: UnitTest");
        //    Assert.IsNotNull(lineItemTradeAllowanceCharge);
        //    Assert.IsTrue(lineItemTradeAllowanceCharge.ChargeIndicator);
        //    Assert.AreEqual(10m, lineItemTradeAllowanceCharge.BasisAmount);
        //    Assert.AreEqual(50m, lineItemTradeAllowanceCharge.ActualAmount);
        //    Assert.AreEqual("Reason: UnitTest", lineItemTradeAllowanceCharge.Reason);
        //}

        ///// <summary>
        ///// This test ensure that BIC won't be written if empty
        ///// </summary>
        //[Fact]
        //public void TestFinancialInstitutionBICEmpty()
        //{
        //    DateTime issueDateTime = DateTime.Today;

        //    InvoiceDescriptor desc = InvoiceProvider.CreateInvoice();
        //    //PayeeSpecifiedCreditorFinancialInstitution
        //    desc.CreditorBankAccounts[0].BIC = string.Empty;
        //    //PayerSpecifiedDebtorFinancialInstitution
        //    desc.AddDebitorFinancialAccount("DE02120300000000202051", string.Empty);

        //    var ms = new MemoryStream();
        //    desc.Save(ms, ZUGFeRDVersion.Version23, Profile.Comfort);

        //    ms.Seek(0, SeekOrigin.Begin);
        //    var reader = new StreamReader(ms);
        //    var text = reader.ReadToEnd();

        //    ms.Seek(0, SeekOrigin.Begin);
        //    var doc = new XmlDocument();
        //    doc.Load(ms);
        //    var nsmgr = new XmlNamespaceManager(doc.DocumentElement.OwnerDocument.NameTable);
        //    nsmgr.AddNamespace("qdt", "urn:un:unece:uncefact:data:standard:QualifiedDataType:100");
        //    nsmgr.AddNamespace("a", "urn:un:unece:uncefact:data:standard:QualifiedDataType:100");
        //    nsmgr.AddNamespace("rsm", "urn:un:unece:uncefact:data:standard:CrossIndustryInvoice:100");
        //    nsmgr.AddNamespace("ram", "urn:un:unece:uncefact:data:standard:ReusableAggregateBusinessInformationEntity:100");
        //    nsmgr.AddNamespace("udt", "urn:un:unece:uncefact:data:standard:UnqualifiedDataType:100");

        //    XmlNodeList creditorFinancialInstitutions = doc.SelectNodes("//ram:ApplicableHeaderTradeSettlement/ram:SpecifiedTradeSettlementPaymentMeans/ram:PayeeSpecifiedCreditorFinancialInstitution", nsmgr);
        //    XmlNodeList debitorFinancialInstitutions = doc.SelectNodes("//ram:ApplicableHeaderTradeSettlement/ram:SpecifiedTradeSettlementPaymentMeans/ram:PayerSpecifiedDebtorFinancialInstitution", nsmgr);

        //    Assert.AreEqual(creditorFinancialInstitutions.Count, 0);
        //    Assert.AreEqual(debitorFinancialInstitutions.Count, 0);
        //} // !TestFinancialInstitutionBICEmpty()


        ///// <summary>
        ///// This test ensure that BIC won't be written if empty
        ///// </summary>
        //[Fact]
        //public void TestAltteilSteuer()
        //{
        //    var desc = InvoiceDescriptor.CreateInvoice("112233", new DateTime(2021, 04, 23), CurrencyCodes.EUR);
        //    desc.Notes.Clear();
        //    desc.Notes.Add(new Note("Rechnung enthält 100 EUR (Umsatz)Steuer auf Altteile gem. Abschn. 10.5 Abs. 3 UStAE", SubjectCodes.ADU, ContentCodes.Unknown));

        //    desc.TradeLineItems.Clear();
        //    desc.AddTradeLineItem(name: "Neumotor",
        //                          unitCode: QuantityCodes.C62,
        //                          unitQuantity: 1,
        //                          billedQuantity: 1,
        //                          netUnitPrice: 1000,
        //                          taxType: TaxTypes.VAT,
        //                          categoryCode: TaxCategoryCodes.S,
        //                          taxPercent: 19);

        //    desc.AddTradeLineItem(name: "Bemessungsgrundlage und Umsatzsteuer auf Altteil",
        //                          unitCode: QuantityCodes.C62,
        //                          unitQuantity: 1,
        //                          billedQuantity: 1,
        //                          netUnitPrice: 100,
        //                          taxType: TaxTypes.VAT,
        //                          categoryCode: TaxCategoryCodes.S,
        //                          taxPercent: 19);

        //    desc.AddTradeLineItem(name: "Korrektur/Stornierung Bemessungsgrundlage der Umsatzsteuer auf Altteil",
        //                          unitCode: QuantityCodes.C62,
        //                          unitQuantity: 1,
        //                          billedQuantity: -1,
        //                          netUnitPrice: 100,
        //                          taxType: TaxTypes.VAT,
        //                          categoryCode: TaxCategoryCodes.Z,
        //                          taxPercent: 0);

        //    desc.AddApplicableTradeTax(basisAmount: 1000,
        //                               percent: 19,
        //                               TaxTypes.VAT,
        //                               TaxCategoryCodes.S);

        //    desc.SetTotals(lineTotalAmount: 1500m,
        //             taxBasisAmount: 1500m,
        //             taxTotalAmount: 304m,
        //             grandTotalAmount: 1804m,
        //             duePayableAmount: 1804m
        //            );

        //    var ms = new MemoryStream();

        //    desc.Save(ms, ZUGFeRDVersion.Version23, Profile.XRechnung);
        //    ms.Seek(0, SeekOrigin.Begin);

        //    var loadedInvoice = InvoiceDescriptor.Load(ms);
        //    Assert.AreEqual(loadedInvoice.Invoicee, null);
        //} // !TestAltteilSteuer()

        //[Fact]
        //public void TestBasisQuantityStandard()
        //{
        //    InvoiceDescriptor desc = InvoiceProvider.CreateInvoice();

        //    desc.TradeLineItems.Clear();
        //    desc.AddTradeLineItem(name: "Joghurt Banane",
        //                          unitCode: QuantityCodes.H87,
        //                          sellerAssignedID: "ARNR2",
        //                          id: new GlobalID(GlobalIDSchemeIdentifiers.EAN, "4000050986428"),
        //                          grossUnitPrice: 5.5m,
        //                          netUnitPrice: 5.5m,
        //                          billedQuantity: 50,
        //                          taxType: TaxTypes.VAT,
        //                          categoryCode: TaxCategoryCodes.S,
        //                          taxPercent: 7
        //                          );
        //    var ms = new MemoryStream();

        //    desc.Save(ms, ZUGFeRDVersion.Version23, Profile.XRechnung);
        //    ms.Seek(0, SeekOrigin.Begin);

        //    var doc = new XmlDocument();
        //    doc.Load(ms);
        //    var nsmgr = new XmlNamespaceManager(doc.DocumentElement.OwnerDocument.NameTable);
        //    nsmgr.AddNamespace("qdt", "urn:un:unece:uncefact:data:standard:QualifiedDataType:100");
        //    nsmgr.AddNamespace("a", "urn:un:unece:uncefact:data:standard:QualifiedDataType:100");
        //    nsmgr.AddNamespace("rsm", "urn:un:unece:uncefact:data:standard:CrossIndustryInvoice:100");
        //    nsmgr.AddNamespace("ram", "urn:un:unece:uncefact:data:standard:ReusableAggregateBusinessInformationEntity:100");
        //    nsmgr.AddNamespace("udt", "urn:un:unece:uncefact:data:standard:UnqualifiedDataType:100");

        //    XmlNode node = doc.SelectSingleNode("//ram:SpecifiedTradeSettlementLineMonetarySummation//ram:LineTotalAmount", nsmgr);
        //    Assert.AreEqual("275.00", node.InnerText);
        //} // !TestBasisQuantityStandard()


        //[Fact]
        //public void TestBasisQuantityMultiple()
        //{
        //    InvoiceDescriptor desc = InvoiceProvider.CreateInvoice();

        //    desc.TradeLineItems.Clear();
        //    TradeLineItem tli = desc.AddTradeLineItem(name: "Joghurt Banane",
        //                                              unitCode: QuantityCodes.H87,
        //                                              sellerAssignedID: "ARNR2",
        //                                              id: new GlobalID(GlobalIDSchemeIdentifiers.EAN, "4000050986428"),
        //                                              grossUnitPrice: 5.5m,
        //                                              netUnitPrice: 5.5m,
        //                                              billedQuantity: 50,
        //                                              taxType: TaxTypes.VAT,
        //                                              categoryCode: TaxCategoryCodes.S,
        //                                              taxPercent: 7,
        //                                              unitQuantity: 10
        //                                              );
        //    var ms = new MemoryStream();

        //    desc.Save(ms, ZUGFeRDVersion.Version23, Profile.XRechnung);
        //    ms.Seek(0, SeekOrigin.Begin);

        //    var doc = new XmlDocument();
        //    doc.Load(ms);
        //    var nsmgr = new XmlNamespaceManager(doc.DocumentElement.OwnerDocument.NameTable);
        //    nsmgr.AddNamespace("qdt", "urn:un:unece:uncefact:data:standard:QualifiedDataType:100");
        //    nsmgr.AddNamespace("a", "urn:un:unece:uncefact:data:standard:QualifiedDataType:100");
        //    nsmgr.AddNamespace("rsm", "urn:un:unece:uncefact:data:standard:CrossIndustryInvoice:100");
        //    nsmgr.AddNamespace("ram", "urn:un:unece:uncefact:data:standard:ReusableAggregateBusinessInformationEntity:100");
        //    nsmgr.AddNamespace("udt", "urn:un:unece:uncefact:data:standard:UnqualifiedDataType:100");

        //    XmlNode node = doc.SelectSingleNode("//ram:SpecifiedTradeSettlementLineMonetarySummation//ram:LineTotalAmount", nsmgr);
        //    Assert.AreEqual("27.50", node.InnerText);
        //} // !TestBasisQuantityMultiple()


        //[Fact]
        //public void TestTradeAllowanceChargeWithoutExplicitPercentage()
        //{
        //    InvoiceDescriptor invoice = InvoiceProvider.CreateInvoice();

        //    // fake values, does not matter for our test case
        //    invoice.AddTradeAllowanceCharge(true, 100, CurrencyCodes.EUR, 10, string.Empty, TaxTypes.VAT, TaxCategoryCodes.S, 19);

        //    var ms = new MemoryStream();
        //    invoice.Save(ms, ZUGFeRDVersion.Version23, Profile.Extended);
        //    ms.Position = 0;

        //    var loadedInvoice = InvoiceDescriptor.Load(ms);
        //    IList<TradeAllowanceCharge> allowanceCharges = loadedInvoice.GetTradeAllowanceCharges();

        //    Assert.IsTrue(allowanceCharges.Count == 1);
        //    Assert.AreEqual(allowanceCharges[0].BasisAmount, 100m);
        //    Assert.AreEqual(allowanceCharges[0].Amount, 10m);
        //    Assert.AreEqual(allowanceCharges[0].ChargePercentage, null);
        //} // !TestTradeAllowanceChargeWithoutExplicitPercentage()


        //[Fact]
        //public void TestTradeAllowanceChargeWithExplicitPercentage()
        //{
        //    InvoiceDescriptor invoice = InvoiceProvider.CreateInvoice();

        //    // fake values, does not matter for our test case
        //    invoice.AddTradeAllowanceCharge(true, 100, CurrencyCodes.EUR, 10, 12, string.Empty, TaxTypes.VAT, TaxCategoryCodes.S, 19);

        //    var ms = new MemoryStream();
        //    invoice.Save(ms, ZUGFeRDVersion.Version23, Profile.Extended);
        //    ms.Position = 0;
        //    var loadedInvoice = InvoiceDescriptor.Load(ms);
        //    IList<TradeAllowanceCharge> allowanceCharges = loadedInvoice.GetTradeAllowanceCharges();

        //    Assert.IsTrue(allowanceCharges.Count == 1);
        //    Assert.AreEqual(allowanceCharges[0].BasisAmount, 100m);
        //    Assert.AreEqual(allowanceCharges[0].Amount, 10m);
        //    Assert.AreEqual(allowanceCharges[0].ChargePercentage, 12);
        //} // !TestTradeAllowanceChargeWithExplicitPercentage()


        //[Fact]
        //public void TestReferenceXRechnung21UBL()
        //{
        //    var path = @"..\..\..\..\demodata\xRechnung\xRechnung UBL.xml";
        //    path = _makeSurePathIsCrossPlatformCompatible(path);

        //    var desc = InvoiceDescriptor.Load(path);

        //    Assert.AreEqual(desc.Profile, Profile.XRechnung);
        //    Assert.AreEqual(desc.Type, InvoiceType.Invoice);

        //    Assert.AreEqual(desc.InvoiceNo, "0815-99-1-a");
        //    Assert.AreEqual(desc.InvoiceDate, new DateTime(2020, 6, 21));
        //    Assert.AreEqual(desc.PaymentReference, "0815-99-1-a");
        //    Assert.AreEqual(desc.OrderNo, "0815-99-1");
        //    Assert.AreEqual(desc.Currency, CurrencyCodes.EUR);

        //    Assert.AreEqual(desc.Buyer.Name, "Rechnungs Roulette GmbH & Co KG");
        //    Assert.AreEqual(desc.Buyer.City, "Klein Schlappstadt a.d. Lusche");
        //    Assert.AreEqual(desc.Buyer.Postcode, "12345");
        //    Assert.AreEqual(desc.Buyer.Country, (CountryCodes)276);
        //    Assert.AreEqual(desc.Buyer.Street, "Beispielgasse 17b");
        //    Assert.AreEqual(desc.Buyer.SpecifiedLegalOrganization.TradingBusinessName, "Rechnungs Roulette GmbH & Co KG");

        //    Assert.AreEqual(desc.BuyerContact.Name, "Manfred Mustermann");
        //    Assert.AreEqual(desc.BuyerContact.EmailAddress, "manfred.mustermann@rr.de");
        //    Assert.AreEqual(desc.BuyerContact.PhoneNo, "012345 98 765 - 44");

        //    Assert.AreEqual(desc.Seller.Name, "Harry Hirsch Holz- und Trockenbau");
        //    Assert.AreEqual(desc.Seller.City, "Klein Schlappstadt a.d. Lusche");
        //    Assert.AreEqual(desc.Seller.Postcode, "12345");
        //    Assert.AreEqual(desc.Seller.Country, (CountryCodes)276);
        //    Assert.AreEqual(desc.Seller.Street, "Beispielgasse 17a");
        //    Assert.AreEqual(desc.Seller.SpecifiedLegalOrganization.TradingBusinessName, "Harry Hirsch Holz- und Trockenbau");

        //    Assert.AreEqual(desc.SellerContact.Name, "Harry Hirsch");
        //    Assert.AreEqual(desc.SellerContact.EmailAddress, "harry.hirsch@hhhtb.de");
        //    Assert.AreEqual(desc.SellerContact.PhoneNo, "012345 78 657 - 8");

        //    Assert.AreEqual(desc.TradeLineItems.Count, 2);

        //    Assert.AreEqual(desc.TradeLineItems[0].SellerAssignedID, "0815");
        //    Assert.AreEqual(desc.TradeLineItems[0].Name, "Leimbinder");
        //    Assert.AreEqual(desc.TradeLineItems[0].Description, "Leimbinder 2x18m; Birke");
        //    Assert.AreEqual(desc.TradeLineItems[0].BilledQuantity, 1);
        //    Assert.AreEqual(desc.TradeLineItems[0].LineTotalAmount, 1245.98m);
        //    Assert.AreEqual(desc.TradeLineItems[0].TaxPercent, 19);

        //    Assert.AreEqual(desc.TradeLineItems[1].SellerAssignedID, "MON");
        //    Assert.AreEqual(desc.TradeLineItems[1].Name, "Montage");
        //    Assert.AreEqual(desc.TradeLineItems[1].Description, "Montage durch Fachpersonal");
        //    Assert.AreEqual(desc.TradeLineItems[1].BilledQuantity, 1);
        //    Assert.AreEqual(desc.TradeLineItems[1].LineTotalAmount, 200.00m);
        //    Assert.AreEqual(desc.TradeLineItems[1].TaxPercent, 7);

        //    Assert.AreEqual(desc.LineTotalAmount, 1445.98m);
        //    Assert.AreEqual(desc.TaxTotalAmount, 250.74m);
        //    Assert.AreEqual(desc.GrandTotalAmount, 1696.72m);
        //    Assert.AreEqual(desc.DuePayableAmount, 1696.72m);

        //    Assert.AreEqual(desc.Taxes[0].TaxAmount, 236.74m);
        //    Assert.AreEqual(desc.Taxes[0].BasisAmount, 1245.98m);
        //    Assert.AreEqual(desc.Taxes[0].Percent, 19);
        //    Assert.AreEqual(desc.Taxes[0].TypeCode, TaxTypes.VAT);
        //    Assert.AreEqual(desc.Taxes[0].CategoryCode, TaxCategoryCodes.S);

        //    Assert.AreEqual(desc.Taxes[1].TaxAmount, 14.0000m);
        //    Assert.AreEqual(desc.Taxes[1].BasisAmount, 200.00m);
        //    Assert.AreEqual(desc.Taxes[1].Percent, 7);
        //    Assert.AreEqual(desc.Taxes[1].TypeCode, TaxTypes.VAT);
        //    Assert.AreEqual(desc.Taxes[1].CategoryCode, TaxCategoryCodes.S);

        //    Assert.AreEqual(desc.GetTradePaymentTerms().FirstOrDefault().DueDate, new DateTime(2020, 6, 21));

        //    Assert.AreEqual(desc.CreditorBankAccounts[0].IBAN, "DE12500105170648489890");
        //    Assert.AreEqual(desc.CreditorBankAccounts[0].BIC, "INGDDEFFXXX");
        //    Assert.AreEqual(desc.CreditorBankAccounts[0].Name, "Harry Hirsch");

        //    Assert.AreEqual(desc.PaymentMeans.TypeCode, (PaymentMeansTypeCodes)30);
        //} // !TestReferenceXRechnung21UBL()

        //[Fact]
        //public void TestWriteAndReadDespatchAdviceDocumentReferenceXRechnung()
        //{
        //    InvoiceDescriptor desc = InvoiceProvider.CreateInvoice();
        //    var despatchAdviceNo = "421567982";
        //    var despatchAdviceDate = new DateTime(2024, 5, 14);
        //    desc.SetDespatchAdviceReferencedDocument(despatchAdviceNo, despatchAdviceDate);

        //    var ms = new MemoryStream();
        //    desc.Save(ms, ZUGFeRDVersion.Version23, Profile.XRechnung);
        //    ms.Seek(0, SeekOrigin.Begin);
        //    var loadedInvoice = InvoiceDescriptor.Load(ms);

        //    Assert.AreEqual(despatchAdviceNo, loadedInvoice.DespatchAdviceReferencedDocument.ID);
        //    Assert.AreEqual(despatchAdviceDate, loadedInvoice.DespatchAdviceReferencedDocument.IssueDateTime);
        //} //!TestWriteAndReadDespatchAdviceDocumentReference

        //[Fact]
        //public void TestSpecifiedTradeAllowanceCharge()
        //{
        //    InvoiceDescriptor invoice = InvoiceProvider.CreateInvoice();

        //    invoice.TradeLineItems[0].AddSpecifiedTradeAllowanceCharge(true, CurrencyCodes.EUR, 198m, 19.8m, 10m, "Discount 10%");

        //    var ms = new MemoryStream();
        //    invoice.Save(ms, ZUGFeRDVersion.Version23, Profile.Extended);
        //    ms.Position = 0;

        //    var loadedInvoice = InvoiceDescriptor.Load(ms);
        //    TradeAllowanceCharge allowanceCharge = loadedInvoice.TradeLineItems[0].GetSpecifiedTradeAllowanceCharges().First();

        //    Assert.AreEqual(allowanceCharge.ChargeIndicator, false);//false = discount
        //    //CurrencyCodes are not written bei InvoiceDescriptor22Writer
        //    //Assert.AreEqual(allowanceCharge.Currency, CurrencyCodes.EUR);
        //    Assert.AreEqual(allowanceCharge.BasisAmount, 198m);
        //    Assert.AreEqual(allowanceCharge.ActualAmount, 19.8m);
        //    Assert.AreEqual(allowanceCharge.ChargePercentage, 10m);
        //    Assert.AreEqual(allowanceCharge.Reason, "Discount 10%");
        //} // !SpecifiedTradeAllowanceCharge()


        //[Fact]
        //public void TestSellerDescription()
        //{
        //    InvoiceDescriptor invoice = InvoiceProvider.CreateInvoice();

        //    var description = "Test description";

        //    invoice.SetSeller(name: "Lieferant GmbH",
        //                      postcode: "80333",
        //                      city: "München",
        //                      street: "Lieferantenstraße 20",
        //                      country: CountryCodes.DE,
        //                      id: string.Empty,
        //                      globalID: new GlobalID(GlobalIDSchemeIdentifiers.GLN, "4000001123452"),
        //                      legalOrganization: new LegalOrganization(GlobalIDSchemeIdentifiers.GLN, "4000001123452", "Lieferant GmbH"),
        //                      description: description
        //                      );

        //    var ms = new MemoryStream();
        //    invoice.Save(ms, ZUGFeRDVersion.Version23, Profile.Extended);
        //    ms.Position = 0;

        //    var loadedInvoice = InvoiceDescriptor.Load(ms);

        //    Assert.AreEqual(loadedInvoice.Seller.Description, description);
        //} // !TestSellerDescription()


        //[Fact]
        //public void TestSellerContact()
        //{
        //    InvoiceDescriptor invoice = InvoiceProvider.CreateInvoice();

        //    var description = "Test description";

        //    invoice.SetSeller(name: "Lieferant GmbH",
        //                      postcode: "80333",
        //                      city: "München",
        //                      street: "Lieferantenstraße 20",
        //                      country: CountryCodes.DE,
        //                      id: string.Empty,
        //                      globalID: new GlobalID(GlobalIDSchemeIdentifiers.GLN, "4000001123452"),
        //                      legalOrganization: new LegalOrganization(GlobalIDSchemeIdentifiers.GLN, "4000001123452", "Lieferant GmbH"),
        //                      description: description
        //                      );

        //    var SELLER_CONTACT = "1-123";
        //    var ORG_UNIT = "2-123";
        //    var EMAIL_ADDRESS = "3-123";
        //    var PHONE_NO = "4-123";
        //    var FAX_NO = "5-123";
        //    invoice.SetSellerContact(SELLER_CONTACT, ORG_UNIT, EMAIL_ADDRESS, PHONE_NO, FAX_NO);

        //    var ms = new MemoryStream();
        //    invoice.Save(ms, ZUGFeRDVersion.Version23, Profile.Extended);
        //    ms.Position = 0;

        //    var loadedInvoice = InvoiceDescriptor.Load(ms);

        //    Assert.AreEqual(SELLER_CONTACT, loadedInvoice.SellerContact.Name);
        //    Assert.AreEqual(ORG_UNIT, loadedInvoice.SellerContact.OrgUnit);
        //    Assert.AreEqual(EMAIL_ADDRESS, loadedInvoice.SellerContact.EmailAddress);
        //    Assert.AreEqual(PHONE_NO, loadedInvoice.SellerContact.PhoneNo);
        //    Assert.AreEqual(FAX_NO, loadedInvoice.SellerContact.FaxNo);

        //    Assert.AreEqual(loadedInvoice.Seller.Description, description);
        //} // !TestSellerContact()

        //[Fact]
        //public void ShouldLoadCiiWithoutQdtNamespace()
        //{
        //    var path = @"..\..\..\..\demodata\xRechnung\xRechnung CII - without qdt.xml";
        //    path = _makeSurePathIsCrossPlatformCompatible(path);

        //    var desc = InvoiceDescriptor.Load(path);

        //    Assert.AreEqual(desc.Profile, Profile.XRechnung);
        //    Assert.AreEqual(desc.Type, InvoiceType.Invoice);
        //    Assert.AreEqual(desc.InvoiceNo, "123456XX");
        //    Assert.AreEqual(desc.TradeLineItems.Count, 2);
        //    Assert.AreEqual(desc.LineTotalAmount, 314.86m);
        //} // !ShouldLoadCIIWithoutQdtNamespace()


        //[Fact]
        //public void TestDesignatedProductClassificationWithFullClassification()
        //{
        //    InvoiceDescriptor desc = InvoiceProvider.CreateInvoice();
        //    desc.TradeLineItems.First().AddDesignatedProductClassification(
        //        DesignatedProductClassificationClassCodes.HS,
        //        "List Version ID Value",
        //        "Class Code",
        //        "Class Name");

        //    var ms = new MemoryStream();

        //    desc.Save(ms, ZUGFeRDVersion.Version23, Profile.XRechnung);

        //    // string comparison
        //    ms.Seek(0, SeekOrigin.Begin);
        //    var reader = new StreamReader(ms);
        //    var content = reader.ReadToEnd();
        //    Assert.IsTrue(content.Contains("<ram:DesignatedProductClassification>"));
        //    Assert.IsTrue(content.Contains("<ram:ClassCode listID=\"HS\" listVersionID=\"List Version ID Value\">Class Code</ram:ClassCode>"));
        //    Assert.IsTrue(content.Contains("<ram:ClassName>Class Name</ram:ClassName>"));

        //    // structure comparison
        //    ms.Seek(0, SeekOrigin.Begin);
        //    var loadedInvoice = InvoiceDescriptor.Load(ms);

        //    Assert.AreEqual(DesignatedProductClassificationClassCodes.HS, loadedInvoice.TradeLineItems.First().GetDesignatedProductClassifications().First().ListID);
        //    Assert.AreEqual("List Version ID Value", loadedInvoice.TradeLineItems.First().GetDesignatedProductClassifications().First().ListVersionID);
        //    Assert.AreEqual("Class Code", loadedInvoice.TradeLineItems.First().GetDesignatedProductClassifications().First().ClassCode);
        //    Assert.AreEqual("Class Name", loadedInvoice.TradeLineItems.First().GetDesignatedProductClassifications().First().ClassName);
        //} // !TestDesignatedProductClassificationWithFullClassification()


        //[Fact]
        //public void TestDesignatedProductClassificationWithEmptyVersionId()
        //{
        //    // test with empty version id value
        //    InvoiceDescriptor desc = InvoiceProvider.CreateInvoice();
        //    desc.TradeLineItems.First().AddDesignatedProductClassification(
        //        DesignatedProductClassificationClassCodes.HS,
        //        null,
        //        "Class Code",
        //        "Class Name"
        //        );

        //    var ms = new MemoryStream();

        //    desc.Save(ms, ZUGFeRDVersion.Version23, Profile.XRechnung);

        //    ms.Seek(0, SeekOrigin.Begin);
        //    var loadedInvoice = InvoiceDescriptor.Load(ms);

        //    Assert.AreEqual(DesignatedProductClassificationClassCodes.HS, desc.TradeLineItems.First().GetDesignatedProductClassifications().First().ListID);
        //    Assert.IsNull(desc.TradeLineItems.First().GetDesignatedProductClassifications().First().ListVersionID);
        //    Assert.AreEqual("Class Code", desc.TradeLineItems.First().GetDesignatedProductClassifications().First().ClassCode);
        //    Assert.AreEqual("Class Name", desc.TradeLineItems.First().GetDesignatedProductClassifications().First().ClassName);
        //} // !TestDesignatedProductClassificationWithEmptyVersionId()



        //[Fact]
        //public void TestDesignatedProductClassificationWithEmptyListIdAndVersionId()
        //{
        //    // test with empty version id value
        //    InvoiceDescriptor desc = InvoiceProvider.CreateInvoice();
        //    desc.TradeLineItems.First().AddDesignatedProductClassification(
        //        DesignatedProductClassificationClassCodes.HS,
        //        null,
        //        "Class Code"
        //        );

        //    var ms = new MemoryStream();

        //    desc.Save(ms, ZUGFeRDVersion.Version23, Profile.XRechnung);

        //    ms.Seek(0, SeekOrigin.Begin);
        //    var loadedInvoice = InvoiceDescriptor.Load(ms);

        //    var x = loadedInvoice.TradeLineItems.First().GetDesignatedProductClassifications().First();


        //    Assert.AreEqual(DesignatedProductClassificationClassCodes.HS, loadedInvoice.TradeLineItems.First().GetDesignatedProductClassifications().First().ListID);
        //    Assert.AreEqual(string.Empty, loadedInvoice.TradeLineItems.First().GetDesignatedProductClassifications().First().ListVersionID);
        //    Assert.AreEqual("Class Code", loadedInvoice.TradeLineItems.First().GetDesignatedProductClassifications().First().ClassCode);
        //    Assert.AreEqual(string.Empty, loadedInvoice.TradeLineItems.First().GetDesignatedProductClassifications().First().ClassName);
        //} // !TestDesignatedProductClassificationWithEmptyListIdAndVersionId()


        //[Fact]
        //public void TestDesignatedProductClassificationWithoutAnyOptionalInformation()
        //{
        //    // test with empty version id value
        //    InvoiceDescriptor desc = InvoiceProvider.CreateInvoice();
        //    desc.TradeLineItems.First().AddDesignatedProductClassification(DesignatedProductClassificationClassCodes.HS);

        //    var ms = new MemoryStream();

        //    desc.Save(ms, ZUGFeRDVersion.Version23, Profile.XRechnung);

        //    ms.Seek(0, SeekOrigin.Begin);
        //    var loadedInvoice = InvoiceDescriptor.Load(ms);

        //    Assert.AreEqual(DesignatedProductClassificationClassCodes.HS, desc.TradeLineItems.First().GetDesignatedProductClassifications().First().ListID);
        //    Assert.IsNull(desc.TradeLineItems.First().GetDesignatedProductClassifications().First().ListVersionID);
        //    Assert.IsNull(desc.TradeLineItems.First().GetDesignatedProductClassifications().First().ClassCode);
        //    Assert.IsNull(desc.TradeLineItems.First().GetDesignatedProductClassifications().First().ClassName);
        //} // !TestDesignatedProductClassificationWithoutAnyOptionalInformation()


        //[Fact]
        //public void TestPaymentTermsMultiCardinality()
        //{
        //    // Arrange
        //    DateTime timestamp = DateTime.Now.Date;
        //    var desc = InvoiceProvider.CreateInvoice();
        //    desc.GetTradePaymentTerms().Clear();
        //    desc.AddTradePaymentTerms("Zahlbar innerhalb 30 Tagen netto bis 04.04.2018", new DateTime(2018, 4, 4));
        //    desc.AddTradePaymentTerms("3% Skonto innerhalb 10 Tagen bis 15.03.2018", new DateTime(2018, 3, 15), PaymentTermsType.Skonto, 10, 3m);
        //    desc.GetTradePaymentTerms().FirstOrDefault().DueDate = timestamp.AddDays(14);

        //    var ms = new MemoryStream();
        //    desc.Save(ms, ZUGFeRDVersion.Version23, Profile.Extended);

        //    ms.Seek(0, SeekOrigin.Begin);
        //    var reader = new StreamReader(ms);
        //    var text = reader.ReadToEnd();

        //    ms.Seek(0, SeekOrigin.Begin);
        //    Assert.AreEqual(InvoiceDescriptor.GetVersion(ms), ZUGFeRDVersion.Version23);

        //    // Act
        //    ms.Seek(0, SeekOrigin.Begin);
        //    var loadedInvoice = InvoiceDescriptor.Load(ms);

        //    // Assert
        //    // PaymentTerms
        //    var paymentTerms = loadedInvoice.GetTradePaymentTerms();
        //    Assert.IsNotNull(paymentTerms);
        //    Assert.AreEqual(2, paymentTerms.Count);
        //    var paymentTerm = loadedInvoice.GetTradePaymentTerms().FirstOrDefault(i => i.Description.StartsWith("Zahlbar"));
        //    Assert.IsNotNull(paymentTerm);
        //    Assert.AreEqual("Zahlbar innerhalb 30 Tagen netto bis 04.04.2018", paymentTerm.Description);
        //    Assert.AreEqual(timestamp.AddDays(14), paymentTerm.DueDate);

        //    paymentTerm = loadedInvoice.GetTradePaymentTerms().FirstOrDefault(i => i.PaymentTermsType == PaymentTermsType.Skonto);
        //    Assert.IsNotNull(paymentTerm);
        //    Assert.AreEqual("3% Skonto innerhalb 10 Tagen bis 15.03.2018", paymentTerm.Description);
        //    // Assert.AreEqual(10, paymentTerm.DueDays);
        //    Assert.AreEqual(3m, paymentTerm.Percentage);
        //} // !TestPaymentTermsMultiCardinality()


        //[Fact]
        //public void TestPaymentTermsSingleCardinality()
        //{
        //    // Arrange
        //    DateTime timestamp = DateTime.Now.Date;
        //    var desc = InvoiceProvider.CreateInvoice();
        //    desc.GetTradePaymentTerms().Clear();
        //    desc.AddTradePaymentTerms("Zahlbar innerhalb 30 Tagen netto bis 04.04.2018", new DateTime(2018, 4, 4));
        //    desc.AddTradePaymentTerms("3% Skonto innerhalb 10 Tagen bis 15.03.2018", new DateTime(2018, 3, 15), percentage: 3m);
        //    desc.GetTradePaymentTerms().FirstOrDefault().DueDate = timestamp.AddDays(14);

        //    var ms = new MemoryStream();
        //    desc.Save(ms, ZUGFeRDVersion.Version23, Profile.Comfort);

        //    ms.Seek(0, SeekOrigin.Begin);
        //    var reader = new StreamReader(ms);
        //    var text = reader.ReadToEnd();

        //    ms.Seek(0, SeekOrigin.Begin);
        //    Assert.AreEqual(InvoiceDescriptor.GetVersion(ms), ZUGFeRDVersion.Version23);

        //    // Act
        //    ms.Seek(0, SeekOrigin.Begin);
        //    var loadedInvoice = InvoiceDescriptor.Load(ms);

        //    // Assert
        //    // PaymentTerms
        //    var paymentTerms = loadedInvoice.GetTradePaymentTerms();
        //    Assert.IsNotNull(paymentTerms);
        //    Assert.AreEqual(1, paymentTerms.Count);
        //    var paymentTerm = loadedInvoice.GetTradePaymentTerms().FirstOrDefault();
        //    Assert.IsNotNull(paymentTerm);
        //    Assert.AreEqual($"Zahlbar innerhalb 30 Tagen netto bis 04.04.2018{Environment.NewLine}3% Skonto innerhalb 10 Tagen bis 15.03.2018", paymentTerm.Description);
        //    Assert.AreEqual(timestamp.AddDays(14), paymentTerm.DueDate);
        //} // !TestPaymentTermsSingleCardinality()


        //[Fact]
        //public void TestPaymentTermsSingleCardinalityStructured()
        //{
        //    DateTime timestamp = DateTime.Now.Date;
        //    var desc = InvoiceProvider.CreateInvoice();
        //    desc.GetTradePaymentTerms().Clear();
        //    desc.AddTradePaymentTerms(string.Empty, null, PaymentTermsType.Skonto, 14, 2.25m);
        //    desc.AddTradePaymentTerms("Description2", null, PaymentTermsType.Skonto, 28, 1m);
        //    desc.GetTradePaymentTerms().FirstOrDefault().DueDate = timestamp.AddDays(14);

        //    var ms = new MemoryStream();
        //    desc.Save(ms, ZUGFeRDVersion.Version23, Profile.XRechnung);

        //    ms.Seek(0, SeekOrigin.Begin);
        //    var reader = new StreamReader(ms);
        //    var text = reader.ReadToEnd();

        //    ms.Seek(0, SeekOrigin.Begin);
        //    Assert.AreEqual(InvoiceDescriptor.GetVersion(ms), ZUGFeRDVersion.Version23);

        //    // Act
        //    ms.Seek(0, SeekOrigin.Begin);
        //    var loadedInvoice = InvoiceDescriptor.Load(ms);

        //    // Assert
        //    // PaymentTerms
        //    var paymentTerms = loadedInvoice.GetTradePaymentTerms();
        //    Assert.IsNotNull(paymentTerms);
        //    Assert.AreEqual(1, paymentTerms.Count);
        //    var paymentTerm = loadedInvoice.GetTradePaymentTerms().FirstOrDefault();
        //    Assert.IsNotNull(paymentTerm);
        //    Assert.AreEqual($"#SKONTO#TAGE=14#PROZENT=2.25#{XmlConstants.XmlNewLine}Description2{XmlConstants.XmlNewLine}#SKONTO#TAGE=28#PROZENT=1.00#", paymentTerm.Description);
        //    Assert.AreEqual(timestamp.AddDays(14), paymentTerm.DueDate);
        //    //Assert.AreEqual(PaymentTermsType.Skonto, paymentTerm.PaymentTermsType);
        //    //Assert.AreEqual(10, paymentTerm.DueDays);
        //    //Assert.AreEqual(3m, paymentTerm.Percentage);
        //} // !TestPaymentTermsSingleCardinalityStructured()

        //[Fact]
        //public void TestBuyerOrderReferenceLineId()
        //{
        //    var path = @"..\..\..\..\demodata\zugferd22\zugferd_2p2_EXTENDED_Fremdwaehrung-factur-x.xml";
        //    path = _makeSurePathIsCrossPlatformCompatible(path);

        //    Stream s = File.Open(path, FileMode.Open);
        //    var desc = InvoiceDescriptor.Load(s);
        //    s.Close();

        //    Assert.AreEqual(desc.TradeLineItems[0].BuyerOrderReferencedDocument.LineID, "1");
        //    Assert.AreEqual(desc.TradeLineItems[0].BuyerOrderReferencedDocument.ID, "ORDER84359");
        //}

        //[Fact]
        //public void TestRequiredDirectDebitFieldsShouldExist()
        //{
        //    var d = new InvoiceDescriptor();
        //    d.Type = InvoiceType.Invoice;
        //    d.InvoiceNo = "471102";
        //    d.Currency = CurrencyCodes.EUR;
        //    d.InvoiceDate = new DateTime(2018, 3, 5);
        //    d.AddTradeLineItem(
        //        lineID: "1",
        //        id: new GlobalID(GlobalIDSchemeIdentifiers.EAN, "4012345001235"),
        //        sellerAssignedID: "TB100A4",
        //        name: "Trennblätter A4",
        //        billedQuantity: 20m,
        //        unitCode: QuantityCodes.H87,
        //        netUnitPrice: 9.9m,
        //        grossUnitPrice: 11.781m,
        //        categoryCode: TaxCategoryCodes.S,
        //        taxPercent: 19.0m,
        //        taxType: TaxTypes.VAT);
        //    d.SetSeller(
        //        id: null,
        //        globalID: new GlobalID(GlobalIDSchemeIdentifiers.GLN, "4000001123452"),
        //        name: "Lieferant GmbH",
        //        postcode: "80333",
        //        city: "München",
        //        street: "Lieferantenstraße 20",
        //        country: CountryCodes.DE,
        //        legalOrganization: new LegalOrganization(GlobalIDSchemeIdentifiers.GLN, "4000001123452", "Lieferant GmbH"));
        //    d.SetBuyer(
        //        id: "GE2020211",
        //        globalID: new GlobalID(GlobalIDSchemeIdentifiers.GLN, "4000001987658"),
        //        name: "Kunden AG Mitte",
        //        postcode: "69876",
        //        city: "Frankfurt",
        //        street: "Kundenstraße 15",
        //        country: CountryCodes.DE);
        //    d.SetPaymentMeansSepaDirectDebit(
        //        "DE98ZZZ09999999999",
        //        "REF A-123");
        //    d.AddDebitorFinancialAccount(
        //        "DE21860000000086001055",
        //        null);
        //    d.AddTradePaymentTerms(
        //        "Der Betrag in Höhe von EUR 235,62 wird am 20.03.2018 von Ihrem Konto per SEPA-Lastschrift eingezogen.");
        //    d.SetTotals(
        //        198.00m,
        //        0.00m,
        //        0.00m,
        //        198.00m,
        //        37.62m,
        //        235.62m,
        //        0.00m,
        //        235.62m);
        //    d.SellerTaxRegistration.Add(
        //        new TaxRegistration
        //        {
        //            SchemeID = TaxRegistrationSchemeID.FC,
        //            No = "201/113/40209"
        //        });
        //    d.SellerTaxRegistration.Add(
        //        new TaxRegistration
        //        {
        //            SchemeID = TaxRegistrationSchemeID.VA,
        //            No = "DE123456789"
        //        });
        //    d.AddApplicableTradeTax(
        //        198.00m,
        //        19.00m,
        //        TaxTypes.VAT,
        //        TaxCategoryCodes.S);

        //    using (var stream = new MemoryStream())
        //    {
        //        d.Save(stream, ZUGFeRDVersion.Version23, Profile.XRechnung);
        //        stream.Seek(0, SeekOrigin.Begin);

        //        // test the raw xml file
        //        var content = Encoding.UTF8.GetString(stream.ToArray());

        //        Assert.IsTrue(content.Contains($"<ram:CreditorReferenceID>DE98ZZZ09999999999</ram:CreditorReferenceID>"));
        //        Assert.IsTrue(content.Contains($"<ram:DirectDebitMandateID>REF A-123</ram:DirectDebitMandateID>"));
        //    }
        //} // !TestRequiredDirectDebitFieldsShouldExist()

        //[Fact]
        //public void TestInNonDebitInvoiceTheDirectDebitFieldsShouldNotExist()
        //{
        //    var d = new InvoiceDescriptor();
        //    d.Type = InvoiceType.Invoice;
        //    d.InvoiceNo = "471102";
        //    d.Currency = CurrencyCodes.EUR;
        //    d.InvoiceDate = new DateTime(2018, 3, 5);
        //    d.AddTradeLineItem(
        //        lineID: "1",
        //        id: new GlobalID(GlobalIDSchemeIdentifiers.EAN, "4012345001235"),
        //        sellerAssignedID: "TB100A4",
        //        name: "Trennblätter A4",
        //        billedQuantity: 20m,
        //        unitCode: QuantityCodes.H87,
        //        netUnitPrice: 9.9m,
        //        grossUnitPrice: 11.781m,
        //        categoryCode: TaxCategoryCodes.S,
        //        taxPercent: 19.0m,
        //        taxType: TaxTypes.VAT);
        //    d.SetSeller(
        //        id: null,
        //        globalID: new GlobalID(GlobalIDSchemeIdentifiers.GLN, "4000001123452"),
        //        name: "Lieferant GmbH",
        //        postcode: "80333",
        //        city: "München",
        //        street: "Lieferantenstraße 20",
        //        country: CountryCodes.DE,
        //        legalOrganization: new LegalOrganization(GlobalIDSchemeIdentifiers.GLN, "4000001123452", "Lieferant GmbH"));
        //    d.SetBuyer(
        //        id: "GE2020211",
        //        globalID: new GlobalID(GlobalIDSchemeIdentifiers.GLN, "4000001987658"),
        //        name: "Kunden AG Mitte",
        //        postcode: "69876",
        //        city: "Frankfurt",
        //        street: "Kundenstraße 15",
        //        country: CountryCodes.DE);
        //    d.SetPaymentMeans(PaymentMeansTypeCodes.SEPACreditTransfer,
        //        "Information of Payment Means",
        //        "DE98ZZZ09999999999",
        //        "REF A-123");
        //    d.AddDebitorFinancialAccount(
        //        "DE21860000000086001055",
        //        null);
        //    d.AddTradePaymentTerms(
        //        "Der Betrag in Höhe von EUR 235,62 wird am 20.03.2018 von Ihrem Konto per SEPA-Lastschrift eingezogen.");
        //    d.SetTotals(
        //        198.00m,
        //        0.00m,
        //        0.00m,
        //        198.00m,
        //        37.62m,
        //        235.62m,
        //        0.00m,
        //        235.62m);
        //    d.SellerTaxRegistration.Add(
        //        new TaxRegistration
        //        {
        //            SchemeID = TaxRegistrationSchemeID.FC,
        //            No = "201/113/40209"
        //        });
        //    d.SellerTaxRegistration.Add(
        //        new TaxRegistration
        //        {
        //            SchemeID = TaxRegistrationSchemeID.VA,
        //            No = "DE123456789"
        //        });
        //    d.AddApplicableTradeTax(
        //        198.00m,
        //        19.00m,
        //        TaxTypes.VAT,
        //        TaxCategoryCodes.S);

        //    using (var stream = new MemoryStream())
        //    {
        //        d.Save(stream, ZUGFeRDVersion.Version23, Profile.XRechnung);
        //        stream.Seek(0, SeekOrigin.Begin);

        //        // test the raw xml file
        //        var content = Encoding.UTF8.GetString(stream.ToArray());

        //        Assert.IsFalse(content.Contains($"<ram:CreditorReferenceID>DE98ZZZ09999999999</ram:CreditorReferenceID>"));
        //        Assert.IsFalse(content.Contains($"<ram:DirectDebitMandateID>REF A-123</ram:DirectDebitMandateID>"));
        //    }
        //} // !TestInNonDebitInvoiceTheDirectDebitFieldsShouldNotExist()
    }
}