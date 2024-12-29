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
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using FluentAssertions;

namespace s2industries.ZUGFeRD.Tests
{
    public partial class XRechnungUBLTests : TestBase
    {
        private readonly InvoiceProvider InvoiceProvider = new();
        private readonly ZUGFeRDVersion version = ZUGFeRDVersion.Version23;

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
            loadedInvoice.TradeLineItems.Count.Should().Be(6);
            loadedInvoice.TradeLineItems[0].AssociatedDocument.ParentLineID.Should().BeNull();
            loadedInvoice.TradeLineItems[1].AssociatedDocument.ParentLineID.Should().BeNull();
            loadedInvoice.TradeLineItems[2].AssociatedDocument.ParentLineID.Should().Be("2");
            loadedInvoice.TradeLineItems[3].AssociatedDocument.ParentLineID.Should().Be("2");
            loadedInvoice.TradeLineItems[4].AssociatedDocument.ParentLineID.Should().Be("2.2");
            loadedInvoice.TradeLineItems[5].AssociatedDocument.ParentLineID.Should().BeNull();
        }


        [Fact]
        public void TestInvoiceCreation()
        {
            InvoiceDescriptor desc = InvoiceProvider.CreateInvoice();
            var ms = new MemoryStream();

            desc.Save(ms, ZUGFeRDVersion.Version23, Profile.XRechnung, ZUGFeRDFormats.UBL);
            ms.Seek(0, SeekOrigin.Begin);

            var loadedInvoice = InvoiceDescriptor.Load(ms);

            loadedInvoice.Invoicee.Should().BeNull();
            loadedInvoice.Seller.Should().NotBeNull();
            loadedInvoice.Taxes.Count.Should().Be(2);
            loadedInvoice.SellerContact.Name.Should().Be("Max Mustermann");
            loadedInvoice.BuyerContact.Should().BeNull();
        } // !TestInvoiceCreation()


        [Fact]
        public void TestTradeLineItemProductCharacteristics()
        {
            InvoiceDescriptor desc = InvoiceProvider.CreateInvoice();

            desc.TradeLineItems[0].ApplicableProductCharacteristics =
                    [
                        new ApplicableProductCharacteristic()
                        {
                            Description = "Test Description",
                            Value = "1.5 kg"
                        },
                        new ApplicableProductCharacteristic()
                        {
                            Description = "UBL Characterstics 2",
                            Value = "3 kg"
                        },
                    ];

            var ms = new MemoryStream();

            desc.Save(ms, version, Profile.XRechnung, ZUGFeRDFormats.UBL);
            ms.Seek(0, SeekOrigin.Begin);

            var loadedInvoice = InvoiceDescriptor.Load(ms);

            loadedInvoice.TradeLineItems.Should().NotBeNull();
            loadedInvoice.TradeLineItems[0].ApplicableProductCharacteristics.Count.Should().Be(2);
            loadedInvoice.TradeLineItems[0].ApplicableProductCharacteristics[0].Description.Should().Be("Test Description");
            loadedInvoice.TradeLineItems[0].ApplicableProductCharacteristics[1].Value.Should().Be("3 kg");
        } // !TestTradelineitemProductCharacterstics()

        [Fact]
        public void TestSpecialUnitCodes()
        {
            InvoiceDescriptor desc = InvoiceProvider.CreateInvoice();

            desc.TradeLineItems[0].UnitCode = QuantityCodes._4G;
            desc.TradeLineItems[1].UnitCode = QuantityCodes.H87;

            var ms = new MemoryStream();

            desc.Save(ms, version, Profile.XRechnung, ZUGFeRDFormats.UBL);
            ms.Seek(0, SeekOrigin.Begin);

            var loadedInvoice = InvoiceDescriptor.Load(ms);

            // test the raw xml file
            var content = Encoding.UTF8.GetString(ms.ToArray());
            content.Contains("unitCode=\"H87\"", StringComparison.OrdinalIgnoreCase).Should().BeTrue();
            content.Contains("unitCode=\"4G\"", StringComparison.OrdinalIgnoreCase).Should().BeTrue();

            loadedInvoice.TradeLineItems.Should().NotBeNull();
            loadedInvoice.TradeLineItems[0].UnitCode.Should().Be(QuantityCodes._4G);
            loadedInvoice.TradeLineItems[1].UnitCode.Should().Be(QuantityCodes.H87);
        } // !TestSpecialUnitCodes()

        [Fact]
        public void TestTradeLineItemAdditionalDocuments()
        {
            InvoiceDescriptor desc = InvoiceProvider.CreateInvoice();

            desc.TradeLineItems[0].AddAdditionalReferencedDocument("testid", AdditionalReferencedDocumentTypeCode.InvoiceDataSheet, referenceTypeCode: ReferenceTypeCodes.ON);
            desc.TradeLineItems[0].AddAdditionalReferencedDocument("testid2", AdditionalReferencedDocumentTypeCode.InvoiceDataSheet, referenceTypeCode: ReferenceTypeCodes.ON);

            var ms = new MemoryStream();

            desc.Save(ms, version, Profile.XRechnung, ZUGFeRDFormats.UBL);
            ms.Seek(0, SeekOrigin.Begin);

            var loadedInvoice = InvoiceDescriptor.Load(ms);

            loadedInvoice.TradeLineItems.Should().NotBeNull();
            loadedInvoice.TradeLineItems[0].GetAdditionalReferencedDocuments().Count.Should().Be(2);
            loadedInvoice.TradeLineItems[0].GetAdditionalReferencedDocuments()[0].ID.Should().Be("testid");
            loadedInvoice.TradeLineItems[0].GetAdditionalReferencedDocuments()[1].ID.Should().Be("testid2");
        } // !TestTradelineitemAdditionalDocuments()

        /// <summary>
        /// https://github.com/stephanstapel/ZUGFeRD-csharp/issues/319
        /// </summary>
        [Fact]
        public void TestSkippingOfAllowanceChargeBasisAmount()
        {
            // actual values do not matter
            var basisAmount = 123.0m;
            var percent = 11.0m;
            var allowanceChargeBasisAmount = 121.0m;

            InvoiceDescriptor desc = InvoiceProvider.CreateInvoice();
            desc.AddApplicableTradeTax(basisAmount, percent, TaxTypes.LOC, TaxCategoryCodes.K, allowanceChargeBasisAmount);
            var ms = new MemoryStream();

            desc.Save(ms, version, Profile.XRechnung, ZUGFeRDFormats.UBL);
            ms.Seek(0, SeekOrigin.Begin);

            var loadedInvoice = InvoiceDescriptor.Load(ms);

            Tax tax = loadedInvoice.Taxes.FirstOrDefault(t => t.TypeCode == TaxTypes.LOC);
            tax.Should().NotBeNull();
            tax.BasisAmount.Should().Be(basisAmount);
            tax.Percent.Should().Be(percent);
            tax.AllowanceChargeBasisAmount.Should().BeNull();
        } // !TestInvoiceCreation()

        [Fact]
        public void TestAllowanceChargeOnDocumentLevel()
        {
            InvoiceDescriptor desc = InvoiceProvider.CreateInvoice();

            // Test Values
            var isDiscount = true;
            decimal? basisAmount = 123.45m;
            CurrencyCodes currency = CurrencyCodes.EUR;
            var actualAmount = 12.34m;
            var reason = "Gutschrift";
            TaxTypes taxTypeCode = TaxTypes.VAT;
            TaxCategoryCodes taxCategoryCode = TaxCategoryCodes.AA;
            var taxPercent = 19.0m;

            desc.AddTradeAllowanceCharge(isDiscount, basisAmount, currency, actualAmount, reason, taxTypeCode, taxCategoryCode, taxPercent);

            desc.TradeLineItems[0].AddTradeAllowanceCharge(true, CurrencyCodes.EUR, 100, 10, "test");

            var ms = new MemoryStream();

            desc.Save(ms, version, Profile.XRechnung, ZUGFeRDFormats.UBL);
            ms.Seek(0, SeekOrigin.Begin);

            var loadedInvoice = InvoiceDescriptor.Load(ms);

            TradeAllowanceCharge loadedAllowanceCharge = loadedInvoice.GetTradeAllowanceCharges()[0];

            loadedInvoice.GetTradeAllowanceCharges().Count.Should().Be(1);
            loadedAllowanceCharge.ChargeIndicator.Should().Be(!isDiscount);
            loadedAllowanceCharge.BasisAmount.Should().Be(basisAmount);
            loadedAllowanceCharge.Currency.Should().Be(currency);
            loadedAllowanceCharge.ActualAmount.Should().Be(actualAmount);
            loadedAllowanceCharge.Reason.Should().Be(reason);
            loadedAllowanceCharge.Tax.TypeCode.Should().Be(taxTypeCode);
            loadedAllowanceCharge.Tax.CategoryCode.Should().Be(taxCategoryCode);
            loadedAllowanceCharge.Tax.Percent.Should().Be(taxPercent);
        } // !TestAllowanceChargeOnDocumentLevel

        [Fact]
        public void TestInvoiceWithAttachment()
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

            desc.Save(ms, version, Profile.XRechnung, ZUGFeRDFormats.UBL);
            ms.Seek(0, SeekOrigin.Begin);

            var loadedInvoice = InvoiceDescriptor.Load(ms);

            loadedInvoice.AdditionalReferencedDocuments.Count.Should().Be(1);

            foreach (AdditionalReferencedDocument document in loadedInvoice.AdditionalReferencedDocuments)
            {
                if (document.ID == "My-File")
                {
                    document.AttachmentBinaryObject.Should().BeEquivalentTo(data);
                    document.Filename.Should().Be(filename);
                    break;
                }
            }
        } // !TestInvoiceWithAttachment()

        [Fact]
        public void TestActualDeliveryDateWithoutDeliveryAddress()
        {
            var timestamp = new DateTime(2024, 08, 11);
            InvoiceDescriptor desc = InvoiceProvider.CreateInvoice();
            var ms = new MemoryStream();

            desc.ActualDeliveryDate = timestamp;

            desc.Save(ms, ZUGFeRDVersion.Version23, Profile.XRechnung, ZUGFeRDFormats.UBL);
            ms.Seek(0, SeekOrigin.Begin);

            var loadedInvoice = InvoiceDescriptor.Load(ms);

            // test the ActualDeliveryDate
            loadedInvoice.ActualDeliveryDate.Should().Be(timestamp);
            loadedInvoice.ShipTo.Should().BeNull();
        } // !TestActualDeliveryDateWithoutDeliveryAddress()

        [Fact]
        public void TestActualDeliveryDateWithDeliveryAddress()
        {
            var timestamp = new DateTime(2024, 08, 11);
            InvoiceDescriptor desc = InvoiceProvider.CreateInvoice();
            var ms = new MemoryStream();

            desc.ActualDeliveryDate = timestamp;

            var shipToID = "1234";
            var shipToName = "Test ShipTo Name";
            CountryCodes shipToCountry = CountryCodes.DE;

            desc.ShipTo = new Party()
            {
                ID = new GlobalID()
                {
                    ID = shipToID
                },
                Name = shipToName,
                Country = shipToCountry
            };

            desc.Save(ms, ZUGFeRDVersion.Version23, Profile.XRechnung, ZUGFeRDFormats.UBL);
            ms.Seek(0, SeekOrigin.Begin);

            var loadedInvoice = InvoiceDescriptor.Load(ms);

            // test the ActualDeliveryDate
            loadedInvoice.ActualDeliveryDate.Should().Be(timestamp);
            loadedInvoice.ShipTo.Should().NotBeNull();
            loadedInvoice.ShipTo.ID.Should().NotBeNull();
            loadedInvoice.ShipTo.ID.ID.Should().Be(shipToID);
            loadedInvoice.ShipTo.Name.Should().Be(shipToName);
            loadedInvoice.ShipTo.Country.Should().Be(shipToCountry);
        } // !TestActualDeliveryDateWithDeliveryAddress()

        [Fact]
        public void TestActualDeliveryAddressWithoutDeliveryDate()
        {
            InvoiceDescriptor desc = InvoiceProvider.CreateInvoice();
            var ms = new MemoryStream();

            // ActualDeliveryDate is set by the InvoiceProvider, we are resetting it to the default value
            desc.ActualDeliveryDate = null;

            var shipToID = "1234";
            var shipToName = "Test ShipTo Name";
            CountryCodes shipToCountry = CountryCodes.DE;

            desc.ShipTo = new Party()
            {
                ID = new GlobalID()
                {
                    ID = shipToID
                },
                Name = shipToName,
                Country = shipToCountry
            };

            desc.Save(ms, ZUGFeRDVersion.Version23, Profile.XRechnung, ZUGFeRDFormats.UBL);
            ms.Seek(0, SeekOrigin.Begin);

            var loadedInvoice = InvoiceDescriptor.Load(ms);

            // test the ActualDeliveryDate
            loadedInvoice.ActualDeliveryDate.Should().BeNull();
            loadedInvoice.ShipTo.Should().NotBeNull();
            loadedInvoice.ShipTo.ID.Should().NotBeNull();
            loadedInvoice.ShipTo.ID.ID.Should().Be(shipToID);
            loadedInvoice.ShipTo.Name.Should().Be(shipToName);
            loadedInvoice.ShipTo.Country.Should().Be(shipToCountry);
        } // !TestActualDeliveryAddressWithoutDeliveryDate()

        [Fact]
        public void TestTaxTypes()
        {
            InvoiceDescriptor desc = InvoiceProvider.CreateInvoice();
            var ms = new MemoryStream();

            desc.Save(ms, ZUGFeRDVersion.Version23, Profile.XRechnung, ZUGFeRDFormats.UBL);
            ms.Seek(0, SeekOrigin.Begin);

            var loadedInvoice = InvoiceDescriptor.Load(ms);

            // test writing and parsing
            loadedInvoice.Taxes.Count.Should().Be(2);
            loadedInvoice.Taxes.All(t => t.TypeCode == TaxTypes.VAT).Should().BeTrue();

            // test the raw xml file
            var content = Encoding.UTF8.GetString(ms.ToArray());
            content.Contains("<cbc:ID>VA</cbc:ID>", StringComparison.OrdinalIgnoreCase).Should().BeFalse();
            content.Contains("<cbc:ID>VAT</cbc:ID>", StringComparison.OrdinalIgnoreCase).Should().BeTrue();

            content.Contains("<cbc:ID>FC</cbc:ID>", StringComparison.OrdinalIgnoreCase).Should().BeFalse();
            content.Contains("<cbc:ID>ID</cbc:ID>", StringComparison.OrdinalIgnoreCase).Should().BeTrue();
        } // !TestInvoiceCreation()

        /// <summary>
        /// We expect this format:
        ///   <cac:PaymentTerms>
        ///     <cbc:Note>
        ///       #SKONTO#TAGE#14#PROZENT=0.00#BASISBETRAG=123.45#
        ///     </cbc:Note>
        ///   </cac:PaymentTerms>
        /// </summary>
        [Fact]
        public void TestSingleSkontoForCorrectIndention()
        {
            var desc = InvoiceProvider.CreateInvoice();

            desc.ClearTradePaymentTerms();
            desc.AddTradePaymentTerms("#SKONTO#TAGE#14#PROZENT=0.00#BASISBETRAG=123.45#");

            var ms = new MemoryStream();
            desc.Save(ms, ZUGFeRDVersion.Version23, Profile.XRechnung, ZUGFeRDFormats.UBL);

            var lines = new StreamReader(ms).ReadToEnd().Split([Environment.NewLine], StringSplitOptions.None).ToList();

            var insidePaymentTerms = false;
            var insideCbcNote = false;
            var noteIndentation = -1;

            foreach (var line in lines)
            {
                // Trim the line to remove leading/trailing whitespace
                var trimmedLine = line.Trim();

                if (trimmedLine.StartsWith("<cac:PaymentTerms>", StringComparison.OrdinalIgnoreCase))
                {
                    insidePaymentTerms = true;
                    continue;
                }
                else if (!insidePaymentTerms)
                {
                    continue;
                }

                // Check if we found the opening <cbc:Note>
                if (!insideCbcNote && trimmedLine.StartsWith("<cbc:Note>", StringComparison.OrdinalIgnoreCase))
                {
                    insideCbcNote = true;
                    noteIndentation = line.TakeWhile(char.IsWhiteSpace).Count();
                    noteIndentation.Should().BeGreaterThanOrEqualTo(0, "Indentation for <cbc:Note> should be non-negative.");
                    continue;
                }

                // Check if we found the closing </cbc:Note>
                if (insideCbcNote && trimmedLine.StartsWith("</cbc:Note>", StringComparison.OrdinalIgnoreCase))
                {
                    var endNoteIndentation = line.TakeWhile(char.IsWhiteSpace).Count();
                    endNoteIndentation.Should().Be(noteIndentation); // Ensure closing tag matches indentation
                    insideCbcNote = false;
                    break;
                }

                // After finding <cbc:Note>, check for indentation of the next line
                if (insideCbcNote)
                {
                    var indention = line.TakeWhile(char.IsWhiteSpace).Count();
                    indention.Should().Be(noteIndentation + 2); // Ensure next line is indented one more
                    continue;
                }
            }

            // Assert that we entered and exited the <cbc:Note> block
            insideCbcNote.Should().BeFalse("We should have exited the <cbc:Note> block.");
        } // !TestSingleSkontoForCorrectIndention()

        /// <summary>
        /// We expect this format:
        ///   <cac:PaymentTerms>
        ///     <cbc:Note>
        ///       #SKONTO#TAGE#14#PROZENT=5.00#BASISBETRAG=123.45#
        ///       #SKONTO#TAGE#21#PROZENT=1.00#BASISBETRAG=123.45#
        ///     </cbc:Note>
        ///   </cac:PaymentTerms>
        /// </summary>
        [Fact]
        public void TestMultiSkontoForCorrectIndention()
        {
            var desc = InvoiceProvider.CreateInvoice();

            desc.ClearTradePaymentTerms();
            desc.AddTradePaymentTerms("#SKONTO#TAGE#14#PROZENT=5.00#BASISBETRAG=123.45#");
            desc.AddTradePaymentTerms("#SKONTO#TAGE#21#PROZENT=1.00#BASISBETRAG=123.45#");

            var ms = new MemoryStream();
            desc.Save(ms, ZUGFeRDVersion.Version23, Profile.XRechnung, ZUGFeRDFormats.UBL);

            var lines = new StreamReader(ms).ReadToEnd().Split(new[] { Environment.NewLine }, StringSplitOptions.None).ToList();

            var insidePaymentTerms = false;
            var insideCbcNote = false;
            var noteIndentation = -1;

            foreach (var line in lines)
            {
                // Trim the line to remove leading/trailing whitespace
                var trimmedLine = line.Trim();

                if (trimmedLine.StartsWith("<cac:PaymentTerms>", StringComparison.OrdinalIgnoreCase))
                {
                    insidePaymentTerms = true;
                    continue;
                }
                else if (!insidePaymentTerms)
                {
                    continue;
                }

                // Check if we found the opening <cbc:Note>
                if (!insideCbcNote && trimmedLine.StartsWith("<cbc:Note>", StringComparison.OrdinalIgnoreCase))
                {
                    insideCbcNote = true;
                    noteIndentation = line.TakeWhile(char.IsWhiteSpace).Count();
                    noteIndentation.Should().BeGreaterThanOrEqualTo(0, "Indentation for <cbc:Note> should be non-negative.");
                    continue;
                }

                // Check if we found the closing </cbc:Note>
                if (insideCbcNote && trimmedLine.StartsWith("</cbc:Note>", StringComparison.OrdinalIgnoreCase))
                {
                    var endNoteIndentation = line.TakeWhile(char.IsWhiteSpace).Count();
                    endNoteIndentation.Should().Be(noteIndentation); // Ensure closing tag matches indentation
                    insideCbcNote = false;
                    break;
                }

                // After finding <cbc:Note>, check for indentation of the next line
                if (insideCbcNote)
                {
                    var indention = line.TakeWhile(char.IsWhiteSpace).Count();
                    indention.Should().Be(noteIndentation + 2); // Ensure next line is indented one more
                    continue;
                }
            }

            // Assert that we entered and exited the <cbc:Note> block
            insideCbcNote.Should().BeFalse("We should have exited the <cbc:Note> block.");
        } // !TestMultiSkontoForCorrectIndention()

        [Fact]
        public void TestBuyerOrderReferenceLineId()
        {
            var path = @"..\..\..\..\demodata\xRechnung\xRechnung with LineId.xml";
            path = _makeSurePathIsCrossPlatformCompatible(path);

            Stream s = File.Open(path, FileMode.Open);
            var desc = InvoiceDescriptor.Load(s);
            s.Close();

            desc.TradeLineItems[0].BuyerOrderReferencedDocument.LineID.Should().Be("6171175.1");
        }

        [Fact]
        public void TestMultipleCreditorBankAccounts()
        {
            var iban1 = "DE901213213312311231";
            var iban2 = "DE911213213312311231";
            var bic1 = "BIC-Test";
            var bic2 = "BIC-Test2";

            InvoiceDescriptor desc = InvoiceProvider.CreateInvoice();
            var ms = new MemoryStream();

            desc.CreditorBankAccounts.Clear();

            desc.CreditorBankAccounts.Add(new BankAccount()
            {
                IBAN = iban1,
                BIC = bic1
            });
            desc.CreditorBankAccounts.Add(new BankAccount()
            {
                IBAN = iban2,
                BIC = bic2
            });

            desc.Save(ms, ZUGFeRDVersion.Version23, Profile.XRechnung, ZUGFeRDFormats.UBL);
            ms.Seek(0, SeekOrigin.Begin);

            var loadedInvoice = InvoiceDescriptor.Load(ms);

            // test the BankAccounts
            loadedInvoice.CreditorBankAccounts.Count.Should().Be(2);
            loadedInvoice.CreditorBankAccounts[0].IBAN.Should().Be(iban1);
            loadedInvoice.CreditorBankAccounts[0].BIC.Should().Be(bic1);
            loadedInvoice.CreditorBankAccounts[1].IBAN.Should().Be(iban2);
            loadedInvoice.CreditorBankAccounts[1].BIC.Should().Be(bic2);
        } // !TestMultipleCreditorBankAccounts()

        [Fact]
        public void TestPartyIdentificationForSeller()
        {
            InvoiceDescriptor desc = InvoiceProvider.CreateInvoice();
            var ms = new MemoryStream();

            desc.Save(ms, ZUGFeRDVersion.Version23, Profile.XRechnung, ZUGFeRDFormats.UBL);
            ms.Seek(0, SeekOrigin.Begin);

            var loadedInvoice = InvoiceDescriptor.Load(ms);

            loadedInvoice.SetPaymentMeans(PaymentMeansTypeCodes.SEPACreditTransfer, "Hier sind Informationen", "DE75512108001245126199", "[Mandate reference identifier]");
            var resultStream = new MemoryStream();
            loadedInvoice.Save(resultStream, ZUGFeRDVersion.Version23, Profile.XRechnung, ZUGFeRDFormats.UBL);

            // test the raw xml file
            var doc = new XmlDocument();
            doc.Load(resultStream);
            var nsmgr = new XmlNamespaceManager(doc.NameTable);
            nsmgr.AddNamespace("ubl", "urn:oasis:names:specification:ubl:schema:xsd:Invoice-2");
            nsmgr.AddNamespace("cac", "urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2");
            nsmgr.AddNamespace("cbc", "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2");

            // PartyIdentification may only exist once
            doc.SelectNodes("//cac:AccountingSupplierParty//cac:PartyIdentification", nsmgr)?.Count.Should().Be(1);

            // PartyIdentification may only be contained in AccountingSupplierParty --> only one such node in the document
            doc.SelectNodes("//cac:PartyIdentification", nsmgr)?.Count.Should().Be(1);
        } // !TestPartyIdentificationForSeller()

        [Fact]
        public void TestPartyIdentificationShouldNotExist()
        {
            InvoiceDescriptor desc = InvoiceProvider.CreateInvoice();
            var ms = new MemoryStream();

            desc.Save(ms, ZUGFeRDVersion.Version23, Profile.XRechnung, ZUGFeRDFormats.UBL);
            ms.Seek(0, SeekOrigin.Begin);

            var loadedInvoice = InvoiceDescriptor.Load(ms);

            var resultStream = new MemoryStream();
            loadedInvoice.Save(resultStream, ZUGFeRDVersion.Version23, Profile.XRechnung, ZUGFeRDFormats.UBL);

            // test the raw xml file
            var doc = new XmlDocument();
            doc.Load(resultStream);
            var nsmgr = new XmlNamespaceManager(doc.NameTable);
            nsmgr.AddNamespace("ubl", "urn:oasis:names:specification:ubl:schema:xsd:Invoice-2");
            nsmgr.AddNamespace("cac", "urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2");
            nsmgr.AddNamespace("cbc", "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2");

            doc.SelectNodes("//cac:PartyIdentification", nsmgr)?.Count.Should().Be(0);
        } // !TestPartyIdentificationShouldNotExist()

        [Fact]
        public void TestInDebitInvoiceTheFinancialAccountNameAndFinancialInstitutionBranchShouldNotExist()
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
                unitCode: QuantityCodes.H87,
                netUnitPrice: 9.9m,
                grossUnitPrice: 11.781m,
                categoryCode: TaxCategoryCodes.S,
                taxPercent: 19.0m,
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
                "Der Betrag in Höhe von EUR 235,62 wird am 20.03.2018 von Ihrem Konto per SEPA-Lastschrift eingezogen.");
            d.SetTotals(
                198.00m,
                0.00m,
                0.00m,
                198.00m,
                37.62m,
                235.62m,
                0.00m,
                235.62m);
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
                198.00m,
                19.00m,
                TaxTypes.VAT,
                TaxCategoryCodes.S);

            using var stream = new MemoryStream();
            d.Save(stream, ZUGFeRDVersion.Version23, Profile.XRechnung, ZUGFeRDFormats.UBL);
            stream.Seek(0, SeekOrigin.Begin);

            // test the raw xml file
            var content = Encoding.UTF8.GetString(stream.ToArray());

            PaymentMandateNamePattern().IsMatch(content).Should().BeFalse();
            PaymentMandateBranchPattern().IsMatch(content).Should().BeFalse();
        } // !TestInDebitInvoiceTheFinancialAccountNameShouldNotExist()

        [GeneratedRegex(@"<cac:PaymentMandate.*>.*<cbc:Name.*>.*</cac:PaymentMandate>", RegexOptions.Singleline)]
        private static partial Regex PaymentMandateNamePattern();
        [GeneratedRegex("<cac:PaymentMandate.*>.*<cac:FinancialInstitutionBranch.*></cac:PaymentMandate>", RegexOptions.Singleline)]
        private static partial Regex PaymentMandateBranchPattern();

        [Fact]
        public void TestInDebitInvoiceThePaymentMandateIdShouldExist()
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
                unitCode: QuantityCodes.H87,
                netUnitPrice: 9.9m,
                grossUnitPrice: 11.781m,
                categoryCode: TaxCategoryCodes.S,
                taxPercent: 19.0m,
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
                "Der Betrag in Höhe von EUR 235,62 wird am 20.03.2018 von Ihrem Konto per SEPA-Lastschrift eingezogen.");
            d.SetTotals(
                198.00m,
                0.00m,
                0.00m,
                198.00m,
                37.62m,
                235.62m,
                0.00m,
                235.62m);
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
                198.00m,
                19.00m,
                TaxTypes.VAT,
                TaxCategoryCodes.S);

            using var stream = new MemoryStream();
            d.Save(stream, ZUGFeRDVersion.Version23, Profile.XRechnung, ZUGFeRDFormats.UBL);
            stream.Seek(0, SeekOrigin.Begin);

            // test the raw xml file
            var content = Encoding.UTF8.GetString(stream.ToArray());

            PaymentMandateIdPattern().IsMatch(content).Should().BeTrue();
        } // !TestInDebitInvoiceThePaymentMandateIdShouldExist()

        [GeneratedRegex(@"<cac:PaymentMeans.*>.*<cac:PaymentMandate.*>.*<cbc:ID.*>REF A-123</cbc:ID.*>.*</cac:PaymentMandate>", RegexOptions.Singleline)]
        private static partial Regex PaymentMandateIdPattern();

        [Fact]
        public void TestInvoiceWithoutOrderReferenceShouldNotWriteEmptyOrderReferenceElement()
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
                unitCode: QuantityCodes.H87,
                netUnitPrice: 9.9m,
                grossUnitPrice: 11.781m,
                categoryCode: TaxCategoryCodes.S,
                taxPercent: 19.0m,
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
                "Der Betrag in Höhe von EUR 235,62 wird am 20.03.2018 von Ihrem Konto per SEPA-Lastschrift eingezogen.");
            d.SetTotals(
                198.00m,
                0.00m,
                0.00m,
                198.00m,
                37.62m,
                235.62m,
                0.00m,
                235.62m);
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
                198.00m,
                19.00m,
                TaxTypes.VAT,
                TaxCategoryCodes.S);

            using var stream = new MemoryStream();
            d.Save(stream, ZUGFeRDVersion.Version23, Profile.XRechnung, ZUGFeRDFormats.UBL);
            stream.Seek(0, SeekOrigin.Begin);

            // test the raw xml file
            var content = Encoding.UTF8.GetString(stream.ToArray());

            content.Should().NotContain("OrderReference");
        } // !TestInvoiceWithoutOrderReferenceShouldNotWriteEmptyOrderReferenceElement()

        [Fact]
        public void TestApplicableTradeTaxWithExemption()
        {
            InvoiceDescriptor descriptor = InvoiceProvider.CreateInvoice();
            var taxCount = descriptor.Taxes.Count;
            descriptor.AddApplicableTradeTax(123.00m, 23m, 23m, TaxTypes.VAT, TaxCategoryCodes.S, exemptionReasonCode: TaxExemptionReasonCodes.VATEX_132_2, exemptionReason: "Tax exemption reason");

            var ms = new MemoryStream();
            descriptor.Save(ms, ZUGFeRDVersion.Version23, Profile.XRechnung, ZUGFeRDFormats.UBL);

            ms.Seek(0, SeekOrigin.Begin);

            File.WriteAllBytes("C:\\temp\\test.xml", ms.ToArray());

            ms.Seek(0, SeekOrigin.Begin);
            var loadedInvoice = InvoiceDescriptor.Load(ms);
            loadedInvoice.Should().NotBeNull();

            loadedInvoice.Taxes.Count.Should().Be(taxCount + 1);
            loadedInvoice.Taxes.Last().ExemptionReason.Should().Be("Tax exemption reason");
            loadedInvoice.Taxes.Last().ExemptionReasonCode.Should().Be(TaxExemptionReasonCodes.VATEX_132_2);
        } // !TestApplicableTradeTaxWithExemption()
    }
}
