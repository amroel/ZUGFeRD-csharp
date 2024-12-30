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
namespace s2industries.ZUGFeRD
{
    /// <summary>
    /// ISO/IEC 6523 — Identifier scheme code
    /// </summary>
    public enum GlobalIDSchemeIdentifiers
    {
        /// <summary>
        /// Unknown means, we have a problem ...
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// SIRENE (System Information et Repertoire des Entreprise et des Etablissements)
        /// </summary>
        Sirene = 2,

        /// <summary>
        /// SIRENE (System Information et Repertoire des Entreprise et des Etablissements)
        /// </summary>
        SiretCode = 9,

        /// <summary>
        /// SWIFT (BIC)
        /// </summary>
        Swift = 21,

        /// <summary>
        /// D-U-N-S Number
        /// </summary>
        DUNS = 60,

        /// <summary>
        /// GS1 Global Location Number (GLN)
        /// </summary>
        GLN = 88,

        /// <summary>
        /// GS1 Global Trade Item Number (GTIN, EAN)
        /// </summary>
        EAN = 160,

        /// <summary>
        /// OSCAR (Odette)
        /// </summary>
        ODETTE = 177,

        /// <summary>
        /// Numero d'entreprise / ondernemingsnummer / Unternehmensnummer
        /// </summary>
        CompanyNumber = 208
    }

    /// <summary>
    /// Extension methods for GlobalIDSchemeIdentifiers enum
    /// </summary>
    public static class GlobalIDSchemeIdentifiersExtensions
    {
        /// <summary>
        /// Converts a string to a GlobalIDSchemeIdentifiers enum value
        /// </summary>
        /// <param name="_"></param>
        /// <param name="s">The string representation of the identifier</param>
        /// <returns>The corresponding GlobalIDSchemeIdentifiers enum value</returns>
        public static GlobalIDSchemeIdentifiers FromString(this GlobalIDSchemeIdentifiers _, string s)
        {
            switch (s)
            {
                case "0002": return GlobalIDSchemeIdentifiers.Sirene;
                case "0009": return GlobalIDSchemeIdentifiers.SiretCode;
                case "0021": return GlobalIDSchemeIdentifiers.Swift;
                case "0060": return GlobalIDSchemeIdentifiers.DUNS;
                case "0088": return GlobalIDSchemeIdentifiers.GLN;
                case "0160": return GlobalIDSchemeIdentifiers.EAN;
                case "0177": return GlobalIDSchemeIdentifiers.ODETTE;
                case "0208": return GlobalIDSchemeIdentifiers.CompanyNumber;
                default: return GlobalIDSchemeIdentifiers.Unknown;
            }
        } // !FromString()

        /// <summary>
        /// Converts a GlobalIDSchemeIdentifiers enum value to its string representation
        /// </summary>
        /// <param name="c">The GlobalIDSchemeIdentifiers enum value</param>
        /// <returns>The string representation of the identifier</returns>
        public static string EnumToString(this GlobalIDSchemeIdentifiers c)
        {
            switch (c)
            {
                case GlobalIDSchemeIdentifiers.Sirene: return "0002";
                case GlobalIDSchemeIdentifiers.SiretCode: return "0009";
                case GlobalIDSchemeIdentifiers.Swift: return "0021";
                case GlobalIDSchemeIdentifiers.DUNS: return "0060";
                case GlobalIDSchemeIdentifiers.GLN: return "0088";
                case GlobalIDSchemeIdentifiers.EAN: return "0160";
                case GlobalIDSchemeIdentifiers.ODETTE: return "0177";
                case GlobalIDSchemeIdentifiers.CompanyNumber: return "0208";
                default: return "0000";
            }
        } // !ToString()
    }
}