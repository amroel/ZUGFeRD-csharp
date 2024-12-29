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

using FluentAssertions;

namespace s2industries.ZUGFeRD.Tests
{
    public class ZUGFeRD20Tests : TestBase
    {
        private readonly InvoiceProvider InvoiceProvider = new();

        [Fact]
        public void TestLineStatusCode()
        {
            var path = @"..\..\..\..\demodata\zugferd20\zugferd_2p0_EXTENDED_Warenrechnung.xml";
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

            desc.Save(ms, ZUGFeRDVersion.Version20, Profile.Extended);
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
        public void TestReferenceBasicInvoice()
        {
            var path = @"..\..\..\..\demodata\zugferd20\zugferd_2p0_BASIC_Einfach.xml";
            path = _makeSurePathIsCrossPlatformCompatible(path);

            Stream s = File.Open(path, FileMode.Open);
            var desc = InvoiceDescriptor.Load(s);
            s.Close();

            desc.Profile.Should().Be(Profile.Basic);
            desc.Type.Should().Be(InvoiceType.Invoice);
            desc.InvoiceNo.Should().Be("471102");
            desc.TradeLineItems.Should().HaveCount(1);
            desc.LineTotalAmount.Should().Be(198.0m);
            desc.IsTest.Should().BeFalse();
        } // !TestReferenceBasicInvoice()

        [Fact]
        public void TestReferenceExtendedInvoice()
        {
            var path = @"..\..\..\..\demodata\zugferd20\zugferd_2p0_EXTENDED_Warenrechnung.xml";
            path = _makeSurePathIsCrossPlatformCompatible(path);

            Stream s = File.Open(path, FileMode.Open);
            var desc = InvoiceDescriptor.Load(s);
            s.Close();

            desc.Profile.Should().Be(Profile.Extended);
            desc.Type.Should().Be(InvoiceType.Invoice);
            desc.InvoiceNo.Should().Be("R87654321012345");
            desc.TradeLineItems.Should().HaveCount(6);
            desc.LineTotalAmount.Should().Be(457.20m);
            desc.IsTest.Should().BeTrue();
        } // !TestReferenceExtendedInvoice()

        [Fact]
        public void TestTotalRounding()
        {
            var uuid = Guid.NewGuid().ToString();
            var issueDateTime = DateTime.Today;

            var desc = new InvoiceDescriptor
            {
                ContractReferencedDocument = new ContractReferencedDocument
                {
                    ID = uuid,
                    IssueDateTime = issueDateTime
                }
            };
            desc.SetTotals(1.99m, 0m, 0m, 0m, 0m, 2m, 0m, 2m, 0.01m);

            var msExtended = new MemoryStream();
            desc.Save(msExtended, ZUGFeRDVersion.Version20, Profile.Extended);
            msExtended.Seek(0, SeekOrigin.Begin);

            var loadedInvoice = InvoiceDescriptor.Load(msExtended);
            loadedInvoice.RoundingAmount.Should().Be(0.01m);

            var msBasic = new MemoryStream();
            desc.Save(msBasic, ZUGFeRDVersion.Version20);
            msBasic.Seek(0, SeekOrigin.Begin);

            loadedInvoice = InvoiceDescriptor.Load(msBasic);
            loadedInvoice.RoundingAmount.Should().Be(0m);
        } // !TestTotalRounding()

        [Fact]
        public void TestMissingPropertiesAreNull()
        {
            var path = @"..\..\..\..\demodata\zugferd20\zugferd_2p0_BASIC_Einfach.xml";
            path = _makeSurePathIsCrossPlatformCompatible(path);

            var invoiceDescriptor = InvoiceDescriptor.Load(path);

            invoiceDescriptor.TradeLineItems.Should().AllSatisfy(x =>
            {
                x.BillingPeriodStart.Should().BeNull();
                x.BillingPeriodEnd.Should().BeNull();
            });
        } // !TestMissingPropertiesAreNull()

        [Fact]
        public void TestApplicableProductCharacteristicsEmpty()
        {
            var path = @"..\..\..\..\demodata\zugferd20\zugferd_2p0_BASIC_Einfach.xml";
            path = _makeSurePathIsCrossPlatformCompatible(path);

            var invoiceDescriptor = InvoiceDescriptor.Load(path);

            invoiceDescriptor.TradeLineItems.Should().AllSatisfy(x => x.ApplicableProductCharacteristics.Should().BeEmpty());
        } // !TestMissingPropertListsEmpty()

        [Fact]
        public void TestLoadingSepaPreNotification()
        {
            var path = @"..\..\..\..\demodata\zugferd20\zugferd_2p0_EN16931_SEPA_Prenotification.xml";
            path = _makeSurePathIsCrossPlatformCompatible(path);

            var invoiceDescriptor = InvoiceDescriptor.Load(path);
            invoiceDescriptor.Profile.Should().Be(Profile.Comfort);

            invoiceDescriptor.PaymentMeans.SEPACreditorIdentifier.Should().Be("DE98ZZZ09999999999");
            invoiceDescriptor.PaymentMeans.SEPAMandateReference.Should().Be("REF A-123");
            invoiceDescriptor.DebitorBankAccounts.Should().HaveCount(1);
            invoiceDescriptor.DebitorBankAccounts[0].IBAN.Should().Be("DE21860000000086001055");

            invoiceDescriptor.GetTradePaymentTerms().Should().HaveCount(1);
            invoiceDescriptor.GetTradePaymentTerms().FirstOrDefault()?
                .Description
                .Trim()
                .Should()
                .Be("Der Betrag in Höhe von EUR 529,87 wird am 20.03.2018 von Ihrem Konto per SEPA-Lastschrift eingezogen.");
        } // !TestLoadingSepaPreNotification()

        [Fact]
        public void TestStoringSepaPreNotification()
        {
            var d = new InvoiceDescriptor
            {
                Type = InvoiceType.Invoice,
                InvoiceNo = "471102",
                Currency = CurrencyCodes.EUR,
                InvoiceDate = new DateTime(2018, 3, 5)
            };
            d.AddTradeLineItem(
                lineID: "1",
                id: new GlobalID(GlobalIDSchemeIdentifiers.EAN, "4012345001235"),
                sellerAssignedID: "TB100A4",
                name: "Trennblätter A4",
                billedQuantity: 20m,
                unitCode: QuantityCodes.C62,
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
                unitCode: QuantityCodes.C62,
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
            d.SellerTaxRegistration.Add(new TaxRegistration
            {
                SchemeID = TaxRegistrationSchemeID.FC,
                No = "201/113/40209"
            });
            d.SellerTaxRegistration.Add(new TaxRegistration
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
            d.Save(stream, ZUGFeRDVersion.Version20, Profile.Comfort);

            stream.Seek(0, SeekOrigin.Begin);

            var d2 = InvoiceDescriptor.Load(stream);
            d2.PaymentMeans.SEPACreditorIdentifier.Should().Be("DE98ZZZ09999999999");
            d2.PaymentMeans.SEPAMandateReference.Should().Be("REF A-123");
            d2.DebitorBankAccounts.Should().HaveCount(1);
            d2.DebitorBankAccounts[0].IBAN.Should().Be("DE21860000000086001055");
        } // !TestStoringSepaPreNotification()

        [Fact]
        public void TestPartyExtensions()
        {
            var path = @"..\..\..\..\demodata\zugferd20\zugferd_2p0_BASIC_Einfach.xml";
            path = _makeSurePathIsCrossPlatformCompatible(path);

            Stream s = File.Open(path, FileMode.Open);
            var desc = InvoiceDescriptor.Load(s);
            s.Close();

            desc.Invoicee = new Party() // this information will not be stored in the output file since it is available in Extended profile only
            {
                Name = "Test",
                ContactName = "Max Mustermann",
                Postcode = "83022",
                City = "Rosenheim",
                Street = "Münchnerstraße 123",
                AddressLine3 = "EG links",
                CountrySubdivisionName = "Bayern",
                Country = CountryCodes.DE
            };
            var ms = new MemoryStream();

            desc.Save(ms, ZUGFeRDVersion.Version20, Profile.Extended);
            ms.Seek(0, SeekOrigin.Begin);

            var loadedInvoice = InvoiceDescriptor.Load(ms);
            loadedInvoice.Invoicee.Name.Should().Be("Test");
            loadedInvoice.Invoicee.ContactName.Should().Be("Max Mustermann");
            loadedInvoice.Invoicee.Postcode.Should().Be("83022");
            loadedInvoice.Invoicee.City.Should().Be("Rosenheim");
            loadedInvoice.Invoicee.Street.Should().Be("Münchnerstraße 123");
            loadedInvoice.Invoicee.AddressLine3.Should().Be("EG links");
            loadedInvoice.Invoicee.CountrySubdivisionName.Should().Be("Bayern");
            loadedInvoice.Invoicee.Country.Should().Be(CountryCodes.DE);
        } // !TestMinimumInvoice()

        [Fact]
        public void TestMimeTypeOfEmbeddedAttachment()
        {
            var path = @"..\..\..\..\demodata\zugferd20\zugferd_2p0_EXTENDED_Warenrechnung.xml";
            path = _makeSurePathIsCrossPlatformCompatible(path);

            Stream s = File.Open(path, FileMode.Open);
            var desc = InvoiceDescriptor.Load(s);
            s.Close();

            var filename1 = "myrandomdata.pdf";
            var filename2 = "myrandomdata.bin";
            DateTime timestamp = DateTime.Now.Date;
            var data = new byte[32768];
            new Random().NextBytes(data);

            desc.AddAdditionalReferencedDocument(
                id: "My-File-PDF",
                issueDateTime: timestamp,
                typeCode: AdditionalReferencedDocumentTypeCode.ReferenceDocument,
                name: "EmbeddedPdf",
                attachmentBinaryObject: data,
                filename: filename1);

            desc.AddAdditionalReferencedDocument(
                id: "My-File-BIN",
                issueDateTime: timestamp,
                typeCode: AdditionalReferencedDocumentTypeCode.ReferenceDocument,
                name: "EmbeddedPdf",
                attachmentBinaryObject: data,
                filename: filename2);

            var ms = new MemoryStream();

            desc.Save(ms, ZUGFeRDVersion.Version20, Profile.Extended);
            ms.Seek(0, SeekOrigin.Begin);

            var loadedInvoice = InvoiceDescriptor.Load(ms);

            //One document is already referenced in "zuferd_2p0_EXTENDED_Warenrechnung.xml"
            loadedInvoice.AdditionalReferencedDocuments.Should().HaveCount(3);

            foreach (AdditionalReferencedDocument document in loadedInvoice.AdditionalReferencedDocuments)
            {
                if (document.ID == "My-File-PDF")
                {
                    document.Filename.Should().Be(filename1);
                    document.MimeType.Should().Be("application/pdf");
                    document.IssueDateTime.Should().Be(timestamp);
                }

                if (document.ID == "My-File-BIN")
                {
                    document.Filename.Should().Be(filename2);
                    document.MimeType.Should().Be("application/octet-stream");
                    document.IssueDateTime.Should().Be(timestamp.AddDays(-2));
                }
            }
        } // !TestMimetypeOfEmbeddedAttachment()

        [Fact]
        public void TestOrderInformation()
        {
            var path = @"..\..\..\..\demodata\zugferd20\zugferd_2p0_EXTENDED_Warenrechnung.xml";
            path = _makeSurePathIsCrossPlatformCompatible(path);

            DateTime timestamp = DateTime.Now.Date;

            Stream s = File.Open(path, FileMode.Open);
            var desc = InvoiceDescriptor.Load(s);
            desc.OrderDate = timestamp;
            desc.OrderNo = "12345";
            s.Close();

            var ms = new MemoryStream();
            desc.Save(ms, ZUGFeRDVersion.Version20, Profile.Extended);

            ms.Seek(0, SeekOrigin.Begin);
            var loadedInvoice = InvoiceDescriptor.Load(ms);
            loadedInvoice.OrderDate.Should().Be(timestamp);
            loadedInvoice.OrderNo.Should().Be("12345");
        } // !TestOrderInformation()

        [Fact]
        public void TestSellerOrderReferencedDocument()
        {
            var path = @"..\..\..\..\demodata\zugferd20\zugferd_2p0_EXTENDED_Warenrechnung.xml";
            path = _makeSurePathIsCrossPlatformCompatible(path);

            Stream s = File.Open(path, FileMode.Open);
            var desc = InvoiceDescriptor.Load(s);
            s.Close();

            var uuid = Guid.NewGuid().ToString();
            DateTime issueDateTime = DateTime.Today;

            desc.SellerOrderReferencedDocument = new SellerOrderReferencedDocument()
            {
                ID = uuid,
                IssueDateTime = issueDateTime
            };

            var ms = new MemoryStream();
            desc.Save(ms, ZUGFeRDVersion.Version20, Profile.Extended);

            ms.Seek(0, SeekOrigin.Begin);
            var loadedInvoice = InvoiceDescriptor.Load(ms);

            loadedInvoice.Profile.Should().Be(Profile.Extended);
            loadedInvoice.SellerOrderReferencedDocument.ID.Should().Be(uuid);
            loadedInvoice.SellerOrderReferencedDocument.IssueDateTime.Should().Be(issueDateTime);
        } // !TestSellerOrderReferencedDocument()

        [Fact]
        public void TestWriteAndReadBusinessProcess()
        {
            InvoiceDescriptor desc = InvoiceProvider.CreateInvoice();
            desc.BusinessProcess = "A1";

            var ms = new MemoryStream();
            desc.Save(ms, ZUGFeRDVersion.Version20, Profile.Extended);
            ms.Seek(0, SeekOrigin.Begin);
            var loadedInvoice = InvoiceDescriptor.Load(ms);

            loadedInvoice.BusinessProcess.Should().Be("A1");
        } // !TestWriteAndReadBusinessProcess

        /// <summary>
        /// This test ensure that Writer and Reader uses the same path and namespace for elements
        /// </summary>
        [Fact]
        public void TestWriteAndReadExtended()
        {
            InvoiceDescriptor desc = InvoiceProvider.CreateInvoice();
            var filename2 = "myrandomdata.bin";
            DateTime timestamp = DateTime.Now.Date;
            var data = new byte[32768];
            new Random().NextBytes(data);

            desc.AddAdditionalReferencedDocument(
                id: "My-File-BIN",
                typeCode: AdditionalReferencedDocumentTypeCode.ReferenceDocument,
                issueDateTime: timestamp.AddDays(-2),
                name: "EmbeddedPdf",
                attachmentBinaryObject: data,
                filename: filename2);

            desc.OrderNo = "12345";
            desc.OrderDate = timestamp;

            desc.SetContractReferencedDocument("12345", timestamp);

            desc.SpecifiedProcuringProject = new SpecifiedProcuringProject { ID = "123", Name = "Project 123" };

            desc.ShipTo = new Party
            {
                ID = new GlobalID(GlobalIDSchemeIdentifiers.Unknown, "123"),
                GlobalID = new GlobalID(GlobalIDSchemeIdentifiers.DUNS, "789"),
                Name = "Ship To",
                ContactName = "Max Mustermann",
                Street = "Münchnerstr. 55",
                Postcode = "83022",
                City = "Rosenheim",
                Country = CountryCodes.DE
            };

            desc.ShipFrom = new Party
            {
                ID = new GlobalID(GlobalIDSchemeIdentifiers.Unknown, "123"),
                GlobalID = new GlobalID(GlobalIDSchemeIdentifiers.DUNS, "789"),
                Name = "Ship From",
                ContactName = "Eva Musterfrau",
                Street = "Alpenweg 5",
                Postcode = "83022",
                City = "Rosenheim",
                Country = CountryCodes.DE
            };

            desc.PaymentMeans.SEPACreditorIdentifier = "SepaID";
            desc.PaymentMeans.SEPAMandateReference = "SepaMandat";
            desc.PaymentMeans.FinancialCard = new FinancialCard { Id = "123", CardholderName = "Mustermann" };

            desc.PaymentReference = "PaymentReference";

            desc.Invoicee = new Party()
            {
                Name = "Test",
                ContactName = "Max Mustermann",
                Postcode = "83022",
                City = "Rosenheim",
                Street = "Münchnerstraße 123",
                AddressLine3 = "EG links",
                CountrySubdivisionName = "Bayern",
                Country = CountryCodes.DE
            };

            desc.Payee = new Party() // this information will not be stored in the output file since it is available in Extended profile only
            {
                Name = "Test",
                ContactName = "Max Mustermann",
                Postcode = "83022",
                City = "Rosenheim",
                Street = "Münchnerstraße 123",
                AddressLine3 = "EG links",
                CountrySubdivisionName = "Bayern",
                Country = CountryCodes.DE
            };

            desc.AddDebitorFinancialAccount(iban: "DE02120300000000202052", bic: "BYLADEM1001", bankName: "Musterbank");
            desc.BillingPeriodStart = timestamp;
            desc.BillingPeriodEnd = timestamp.AddDays(14);

            desc.AddTradeAllowanceCharge(false, 5m, CurrencyCodes.EUR, 15m, "Reason for charge", TaxTypes.AAB, TaxCategoryCodes.AB, 19m);
            desc.AddLogisticsServiceCharge(10m, "Logistics service charge", TaxTypes.AAC, TaxCategoryCodes.AC, 7m);

            desc.GetTradePaymentTerms().First().DueDate = timestamp.AddDays(14);
            desc.AddInvoiceReferencedDocument("RE-12345", timestamp);


            //set additional LineItem data
            var lineItem = desc.TradeLineItems.FirstOrDefault(i => i.SellerAssignedID == "TB100A4");
            lineItem.Should().NotBeNull();
            lineItem.Description = "This is line item TB100A4";
            lineItem.BuyerAssignedID = "0815";
            lineItem.SetOrderReferencedDocument("12345", timestamp);
            lineItem.SetDeliveryNoteReferencedDocument("12345", timestamp);
            lineItem.SetContractReferencedDocument("12345", timestamp);

            lineItem.AddAdditionalReferencedDocument("xyz", AdditionalReferencedDocumentTypeCode.ReferenceDocument, ReferenceTypeCodes.AAB, timestamp);

            lineItem.UnitQuantity = 3m;
            lineItem.ActualDeliveryDate = timestamp;

            lineItem.ApplicableProductCharacteristics.Add(new ApplicableProductCharacteristic
            {
                Description = "Product characteristics",
                Value = "Product value"
            });

            lineItem.BillingPeriodStart = timestamp;
            lineItem.BillingPeriodEnd = timestamp.AddDays(10);

            lineItem.AddReceivableSpecifiedTradeAccountingAccount("987654");
            lineItem.AddTradeAllowanceCharge(false, CurrencyCodes.EUR, 10m, 50m, "Reason: UnitTest");


            var ms = new MemoryStream();
            desc.Save(ms, ZUGFeRDVersion.Version20, Profile.Extended);

            ms.Seek(0, SeekOrigin.Begin);
            var reader = new StreamReader(ms);
            var text = reader.ReadToEnd();

            ms.Seek(0, SeekOrigin.Begin);
            var loadedInvoice = InvoiceDescriptor.Load(ms);

            loadedInvoice.InvoiceNo.Should().Be("471102");
            loadedInvoice.InvoiceDate.Should().Be(new DateTime(2018, 03, 05));
            loadedInvoice.Currency.Should().Be(CurrencyCodes.EUR);
            loadedInvoice.Notes.Should().Contain(n => n.Content == "Rechnung gemäß Bestellung vom 01.03.2018.");
            loadedInvoice.ReferenceOrderNo.Should().Be("04011000-12345-34");
            loadedInvoice.Seller.Name.Should().Be("Lieferant GmbH");
            loadedInvoice.Seller.Postcode.Should().Be("80333");
            loadedInvoice.Seller.City.Should().Be("München");

            loadedInvoice.Seller.Street.Should().Be("Lieferantenstraße 20");
            loadedInvoice.Seller.Country.Should().Be(CountryCodes.DE);
            loadedInvoice.Seller.GlobalID.SchemeID.Should().Be(GlobalIDSchemeIdentifiers.GLN);
            loadedInvoice.Seller.GlobalID.ID.Should().Be("4000001123452");
            loadedInvoice.SellerContact.Name.Should().Be("Max Mustermann");
            loadedInvoice.SellerContact.OrgUnit.Should().Be("Muster-Einkauf");
            loadedInvoice.SellerContact.EmailAddress.Should().Be("Max@Mustermann.de");
            loadedInvoice.SellerContact.PhoneNo.Should().Be("+49891234567");

            loadedInvoice.Buyer.Name.Should().Be("Kunden AG Mitte");
            loadedInvoice.Buyer.Postcode.Should().Be("69876");
            loadedInvoice.Buyer.City.Should().Be("Frankfurt");
            loadedInvoice.Buyer.Street.Should().Be("Kundenstraße 15");
            loadedInvoice.Buyer.Country.Should().Be(CountryCodes.DE);
            loadedInvoice.Buyer.ID.ID.Should().Be("GE2020211");

            loadedInvoice.OrderNo.Should().Be("12345");
            loadedInvoice.OrderDate.Should().Be(timestamp);

            //Currently not implemented
            //Assert.AreEqual("12345", loadedInvoice.ContractReferencedDocument.ID);
            //Assert.AreEqual(timestamp, loadedInvoice.ContractReferencedDocument.IssueDateTime);

            //Currently not implemented
            //Assert.AreEqual("123", loadedInvoice.SpecifiedProcuringProject.ID);
            //Assert.AreEqual("Project 123", loadedInvoice.SpecifiedProcuringProject.Name);

            loadedInvoice.ShipTo.ID.ID.Should().Be("123");
            loadedInvoice.ShipTo.GlobalID.SchemeID.Should().Be(GlobalIDSchemeIdentifiers.DUNS);
            loadedInvoice.ShipTo.GlobalID.ID.Should().Be("789");
            loadedInvoice.ShipTo.Name.Should().Be("Ship To");
            loadedInvoice.ShipTo.ContactName.Should().Be("Max Mustermann");
            loadedInvoice.ShipTo.Street.Should().Be("Münchnerstr. 55");
            loadedInvoice.ShipTo.Postcode.Should().Be("83022");
            loadedInvoice.ShipTo.City.Should().Be("Rosenheim");
            loadedInvoice.ShipTo.Country.Should().Be(CountryCodes.DE);

            loadedInvoice.ShipFrom.ID.ID.Should().Be("123");
            loadedInvoice.ShipFrom.GlobalID.SchemeID.Should().Be(GlobalIDSchemeIdentifiers.DUNS);
            loadedInvoice.ShipFrom.GlobalID.ID.Should().Be("789");
            loadedInvoice.ShipFrom.Name.Should().Be("Ship From");
            loadedInvoice.ShipFrom.ContactName.Should().Be("Eva Musterfrau");
            loadedInvoice.ShipFrom.Street.Should().Be("Alpenweg 5");
            loadedInvoice.ShipFrom.Postcode.Should().Be("83022");
            loadedInvoice.ShipFrom.City.Should().Be("Rosenheim");
            loadedInvoice.ShipFrom.Country.Should().Be(CountryCodes.DE);


            loadedInvoice.ActualDeliveryDate.Should().Be(new DateTime(2018, 03, 05));
            loadedInvoice.PaymentMeans.TypeCode.Should().Be(PaymentMeansTypeCodes.SEPACreditTransfer);
            loadedInvoice.PaymentMeans.Information.Should().Be("Zahlung per SEPA Überweisung.");

            loadedInvoice.PaymentReference.Should().Be("PaymentReference");

            loadedInvoice.PaymentMeans.SEPACreditorIdentifier.Should().Be("SepaID");
            loadedInvoice.PaymentMeans.SEPAMandateReference.Should().Be("SepaMandat");
            loadedInvoice.PaymentMeans.FinancialCard.Id.Should().Be("123");
            loadedInvoice.PaymentMeans.FinancialCard.CardholderName.Should().Be("Mustermann");

            var bankAccount = loadedInvoice.CreditorBankAccounts.FirstOrDefault(a => a.IBAN == "DE02120300000000202051");
            bankAccount.Should().NotBeNull();
            bankAccount.Name.Should().Be("Kunden AG");
            bankAccount.IBAN.Should().Be("DE02120300000000202051");
            bankAccount.BIC.Should().Be("BYLADEM1001");

            var debitorBankAccount = loadedInvoice.DebitorBankAccounts.FirstOrDefault(a => a.IBAN == "DE02120300000000202052");
            debitorBankAccount.Should().NotBeNull();
            debitorBankAccount.IBAN.Should().Be("DE02120300000000202052");

            loadedInvoice.Invoicee.Name.Should().Be("Test");
            loadedInvoice.Invoicee.ContactName.Should().Be("Max Mustermann");
            loadedInvoice.Invoicee.Postcode.Should().Be("83022");
            loadedInvoice.Invoicee.City.Should().Be("Rosenheim");
            loadedInvoice.Invoicee.Street.Should().Be("Münchnerstraße 123");
            loadedInvoice.Invoicee.AddressLine3.Should().Be("EG links");
            loadedInvoice.Invoicee.CountrySubdivisionName.Should().Be("Bayern");
            loadedInvoice.Invoicee.Country.Should().Be(CountryCodes.DE);

            loadedInvoice.Payee.Name.Should().Be("Test");
            loadedInvoice.Payee.ContactName.Should().Be("Max Mustermann");
            loadedInvoice.Payee.Postcode.Should().Be("83022");
            loadedInvoice.Payee.City.Should().Be("Rosenheim");
            loadedInvoice.Payee.Street.Should().Be("Münchnerstraße 123");
            loadedInvoice.Payee.AddressLine3.Should().Be("EG links");
            loadedInvoice.Payee.CountrySubdivisionName.Should().Be("Bayern");
            loadedInvoice.Payee.Country.Should().Be(CountryCodes.DE);

            var tax = loadedInvoice.Taxes.FirstOrDefault(t => t.BasisAmount == 275m);
            tax.Should().NotBeNull();
            tax.BasisAmount.Should().Be(275m);
            tax.Percent.Should().Be(7m);
            tax.TypeCode.Should().Be(TaxTypes.VAT);
            tax.CategoryCode.Should().Be(TaxCategoryCodes.S);

            loadedInvoice.BillingPeriodStart.Should().Be(timestamp);
            loadedInvoice.BillingPeriodEnd.Should().Be(timestamp.AddDays(14));

            //TradeAllowanceCharges
            var tradeAllowanceCharge = loadedInvoice.GetTradeAllowanceCharges().FirstOrDefault(i => i.Reason == "Reason for charge");
            tradeAllowanceCharge.Should().NotBeNull();
            tradeAllowanceCharge.ChargeIndicator.Should().BeTrue();
            tradeAllowanceCharge.Reason.Should().Be("Reason for charge");
            tradeAllowanceCharge.BasisAmount.Should().Be(5m);
            tradeAllowanceCharge.ActualAmount.Should().Be(15m);
            tradeAllowanceCharge.Currency.Should().Be(CurrencyCodes.EUR);
            tradeAllowanceCharge.Tax.Percent.Should().Be(19m);
            tradeAllowanceCharge.Tax.TypeCode.Should().Be(TaxTypes.AAB);
            tradeAllowanceCharge.Tax.CategoryCode.Should().Be(TaxCategoryCodes.AB);

            //ServiceCharges
            var serviceCharge = desc.ServiceCharges.FirstOrDefault(i => i.Description == "Logistics service charge");
            serviceCharge.Should().NotBeNull();
            serviceCharge.Description.Should().Be("Logistics service charge");
            serviceCharge.Amount.Should().Be(10m);
            serviceCharge.Tax.Percent.Should().Be(7m);
            serviceCharge.Tax.TypeCode.Should().Be(TaxTypes.AAC);
            serviceCharge.Tax.CategoryCode.Should().Be(TaxCategoryCodes.AC);

            //PaymentTerms
            var paymentTerms = loadedInvoice.GetTradePaymentTerms().FirstOrDefault();
            paymentTerms.Should().NotBeNull();
            paymentTerms.Description.Should().Be("Zahlbar innerhalb 30 Tagen netto bis 04.04.2018, 3% Skonto innerhalb 10 Tagen bis 15.03.2018");
            paymentTerms.DueDate.Should().Be(timestamp.AddDays(14));

            loadedInvoice.LineTotalAmount.Should().Be(473.0m);
            loadedInvoice.ChargeTotalAmount.Should().Be(null); // optional
            loadedInvoice.AllowanceTotalAmount.Should().Be(null); // optional
            loadedInvoice.TaxBasisAmount.Should().Be(473.0m);
            loadedInvoice.TaxTotalAmount.Should().Be(56.87m);
            loadedInvoice.GrandTotalAmount.Should().Be(529.87m);
            loadedInvoice.TotalPrepaidAmount.Should().Be(null); // optional
            loadedInvoice.DuePayableAmount.Should().Be(529.87m);

            //InvoiceReferencedDocument
            loadedInvoice.GetInvoiceReferencedDocuments().First().ID.Should().Be("RE-12345");
            loadedInvoice.GetInvoiceReferencedDocuments().First().IssueDateTime.Should().Be(timestamp);

            //Line items
            var loadedLineItem = loadedInvoice.TradeLineItems.FirstOrDefault(i => i.SellerAssignedID == "TB100A4");
            loadedLineItem.Should().NotBeNull();
            loadedLineItem.AssociatedDocument.LineID.Should().NotBeNullOrWhiteSpace();
            loadedLineItem.Description.Should().Be("This is line item TB100A4");

            loadedLineItem.Name.Should().Be("Trennblätter A4");

            loadedLineItem.SellerAssignedID.Should().Be("TB100A4");
            loadedLineItem.BuyerAssignedID.Should().Be("0815");
            loadedLineItem.GlobalID.SchemeID.Should().Be(GlobalIDSchemeIdentifiers.EAN);
            loadedLineItem.GlobalID.ID.Should().Be("4012345001235");

            //GrossPriceProductTradePrice
            loadedLineItem.GrossUnitPrice.Should().Be(9.9m);
            QuantityCodes.H87.Should().Be(QuantityCodes.H87);
            loadedLineItem.UnitQuantity.Should().Be(3m);

            //NetPriceProductTradePrice
            loadedLineItem.NetUnitPrice.Should().Be(9.9m);
            loadedLineItem.BilledQuantity.Should().Be(20m);

            loadedLineItem.TaxType.Should().Be(TaxTypes.VAT);
            loadedLineItem.TaxCategoryCode.Should().Be(TaxCategoryCodes.S);
            loadedLineItem.TaxPercent.Should().Be(19m);

            loadedLineItem.BuyerOrderReferencedDocument.ID.Should().Be("12345");
            loadedLineItem.BuyerOrderReferencedDocument.IssueDateTime.Should().Be(timestamp);
            loadedLineItem.DeliveryNoteReferencedDocument.ID.Should().Be("12345");
            loadedLineItem.DeliveryNoteReferencedDocument.IssueDateTime.Should().Be(timestamp);
            loadedLineItem.ContractReferencedDocument.ID.Should().Be("12345");
            loadedLineItem.ContractReferencedDocument.IssueDateTime.Should().Be(timestamp);

            var lineItemReferencedDoc = loadedLineItem.GetAdditionalReferencedDocuments().FirstOrDefault();
            lineItemReferencedDoc.Should().NotBeNull();
            lineItemReferencedDoc.ID.Should().Be("xyz");
            lineItemReferencedDoc.TypeCode.Should().Be(AdditionalReferencedDocumentTypeCode.ReferenceDocument);
            lineItemReferencedDoc.IssueDateTime.Should().Be(timestamp);
            lineItemReferencedDoc.ReferenceTypeCode.Should().Be(ReferenceTypeCodes.AAB);

            var productCharacteristics = loadedLineItem.ApplicableProductCharacteristics.FirstOrDefault();
            productCharacteristics.Should().NotBeNull();
            productCharacteristics.Description.Should().Be("Product characteristics");
            productCharacteristics.Value.Should().Be("Product value");

            loadedLineItem.ActualDeliveryDate.Should().Be(timestamp);
            loadedLineItem.BillingPeriodStart.Should().Be(timestamp);
            loadedLineItem.BillingPeriodEnd.Should().Be(timestamp.AddDays(10));

            //Currently not implemented
            //var accountingAccount = loadedLineItem.ReceivableSpecifiedTradeAccountingAccounts.FirstOrDefault();
            //Assert.IsNotNull(accountingAccount);
            //Assert.AreEqual("987654", accountingAccount.TradeAccountID);


            var lineItemTradeAllowanceCharge = loadedLineItem.GetTradeAllowanceCharges().FirstOrDefault(i => i.Reason == "Reason: UnitTest");
            lineItemTradeAllowanceCharge.Should().NotBeNull();
            lineItemTradeAllowanceCharge.ChargeIndicator.Should().BeTrue();
            lineItemTradeAllowanceCharge.BasisAmount.Should().Be(10m);
            lineItemTradeAllowanceCharge.ActualAmount.Should().Be(50m);
            lineItemTradeAllowanceCharge.Reason.Should().Be("Reason: UnitTest");
        }
    }
}
