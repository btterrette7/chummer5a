﻿using Chummer.Annotations;
using Chummer.Backend.Equipment;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Xml;

namespace Chummer.Backend.Attributes
{
    /// <summary>
    /// Character CharacterAttribute.
    /// </summary>
    [DebuggerDisplay("{_strAbbrev}")]
    public class CharacterAttrib : INotifyPropertyChanged
    {
        private int _intMetatypeMin = 1;
        private int _intMetatypeMax = 6;
        private int _intMetatypeAugMax = 9;
        private int _intValue = 0;
        private int _intAugModifier = 0;
        private int _intBase = 0;
        private int _intKarma = 0;
        private string _strAbbrev = "";
        public Character _objCharacter;

        public event PropertyChangedEventHandler PropertyChanged;

        #region Constructor, Save, Load, and Print Methods
        /// <summary>
        /// Character CharacterAttribute.
        /// </summary>
        /// <param name="strAbbrev">CharacterAttribute abbreviation.</param>
        /// <param name="enumCategory"></param>
        public CharacterAttrib(string strAbbrev, AttributeCategory enumCategory = AttributeCategory.Standard)
        {
            _strAbbrev = strAbbrev;
	        Category = enumCategory;
        }

        /// <summary>
        /// Save the object's XML to the XmlWriter.
        /// </summary>
        /// <param name="objWriter">XmlTextWriter to write with.</param>
        public void Save(XmlTextWriter objWriter)
        {
            objWriter.WriteStartElement("attribute");
            objWriter.WriteElementString("name", _strAbbrev);
            objWriter.WriteElementString("metatypemin", _intMetatypeMin.ToString());
            objWriter.WriteElementString("metatypemax", _intMetatypeMax.ToString());
            objWriter.WriteElementString("metatypeaugmax", _intMetatypeAugMax.ToString());
            objWriter.WriteElementString("value", Value.ToString());
            objWriter.WriteElementString("base", _intBase.ToString());
            objWriter.WriteElementString("karma", _intKarma.ToString());
            objWriter.WriteElementString("augmodifier", _intAugModifier.ToString());
			objWriter.WriteElementString("category", Category.ToString());
            // External reader friendly stuff.
            objWriter.WriteElementString("totalvalue", TotalValue.ToString());
            objWriter.WriteEndElement();
        }

        /// <summary>
        /// Load the CharacterAttribute from the XmlNode.
        /// </summary>
        /// <param name="objNode">XmlNode to load.</param>
        public void Load(XmlNode objNode)
        {
            _strAbbrev = objNode["name"].InnerText;
            _intMetatypeMin = Convert.ToInt32(objNode["metatypemin"].InnerText);
            _intMetatypeMax = Convert.ToInt32(objNode["metatypemax"].InnerText);
            _intMetatypeAugMax = Convert.ToInt32(objNode["metatypeaugmax"].InnerText);

            objNode.TryGetField("base", out _intBase);
            objNode.TryGetField("karma", out _intKarma);
			_enumCategory = ConvertToAttributeCategory(objNode["category"]?.InnerText, _strAbbrev);
            _intAugModifier = Convert.ToInt32(objNode["augmodifier"].InnerText);
        }

        /// <summary>
        /// Print the object's XML to the XmlWriter.
        /// </summary>
        /// <param name="objWriter">XmlTextWriter to write with.</param>
        public void Print(XmlTextWriter objWriter)
        {
            objWriter.WriteStartElement("attribute");
            objWriter.WriteElementString("name", _strAbbrev);
            objWriter.WriteElementString("base", Value.ToString());
            objWriter.WriteElementString("total", TotalValue.ToString());
            objWriter.WriteElementString("min", TotalMinimum.ToString());
            objWriter.WriteElementString("max", TotalMaximum.ToString());
            objWriter.WriteElementString("aug", TotalAugmentedMaximum.ToString());
			objWriter.WriteElementString("bp", CalculatedBP().ToString());
			objWriter.WriteElementString("category", Category.ToString());
			objWriter.WriteEndElement();
        }
		#endregion
		/// <summary>
		/// Type of Attribute.
		/// </summary>
		public enum AttributeCategory
		{
			Standard = 0,
			Special = 1,
			Shapeshifter = 2
		}

		#region Properties

	    public Enum Category
	    {
		    get { return _enumCategory; }
			set { _enumCategory = value; }
	    }

		/// <summary>
		/// Minimum value for the CharacterAttribute as set by the character's Metatype.
		/// </summary>
		public int MetatypeMinimum
        {
            get
            {
                int intReturn = _intMetatypeMin;
                foreach (Improvement objImprovement in _objCharacter.Improvements.Where(objImprovement => objImprovement.ImproveType == Improvement.ImprovementType.ReplaceAttribute).Where(objImprovement => objImprovement.ImprovedName == Abbrev))
                {
                    intReturn = objImprovement.Minimum;
                }
                return intReturn;
            }
            set
            {
                _intMetatypeMin = value;
            }
        }

        /// <summary>
        /// Maximum value for the CharacterAttribute as set by the character's Metatype.
        /// </summary>
        public int MetatypeMaximum
        {
            get
            {
                int intReturn = _intMetatypeMax;
                foreach (Improvement objImprovement in _objCharacter.Improvements.Where(objImprovement => objImprovement.ImproveType == Improvement.ImprovementType.ReplaceAttribute).Where(objImprovement => objImprovement.ImprovedName == Abbrev))
                {
                    intReturn = objImprovement.Maximum;
                }
                return intReturn;
            }
            set
            {
                _intMetatypeMax = value;
            }
        }

        /// <summary>
        /// Maximum augmented value for the CharacterAttribute as set by the character's Metatype.
        /// </summary>
        public int MetatypeAugmentedMaximum
        {
            get
            {
                return _intMetatypeAugMax;
            }
            set
            {
                _intMetatypeAugMax = value;
            }
        }

        /// <summary>
        /// Current base value of the CharacterAttribute.
        /// </summary>
        public int Base
        {
            get
            {
                return _intBase;
            }
            set
            {
                _intBase = value;
				OnPropertyChanged(nameof(Value));
			}
        }

        /// <summary>
        /// Current karma value of the CharacterAttribute.
        /// </summary>
        public int Karma
        {
            get
            {
                return _intKarma;
            }
            set
            {
                _intKarma = value;
                OnPropertyChanged(nameof(Value));
            }
        }

        /// <summary>
		/// Current value of the CharacterAttribute.
		/// </summary>
		public int Value
        {
            get
            {
                //This amount of object allocation is uglier than the US national dept, but improvementmanager is due for a rewrite anyway
                ImprovementManager _objImprovement = new ImprovementManager(_objCharacter);
                return Base + Karma + Math.Min(_objImprovement.ValueOf(Improvement.ImprovementType.Attributelevel, false, Abbrev), MetatypeMaximum - MetatypeMinimum);
            }
        }

		/// <summary>
		/// Formatted Value of the attribute, including the sum of any modifiers in brackets.
		/// </summary>
	    public string DisplayValue
	    {
		    get
		    {
			    return HasModifiers ? $"{Value} ({CalculatedTotalValue()})" : $"{Value}";
		    }
	    }

        /// <summary>
        /// Augmentation modifier value for the CharacterAttribute.
        /// </summary>
        /// <remarks>This value should not be saved with the character information. It should instead be re-calculated every time the character is loaded and augmentations are added/removed.</remarks>
        public int AugmentModifier
        {
            get
            {
                return _intAugModifier;
            }
            set
            {
                _intAugModifier = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// The CharacterAttribute's total value including augmentations.
        /// </summary>
        /// <remarks>This value should not be saved with the character information. It should instead be re-calculated every time the character is loaded and augmentations are added/removed.</remarks>
        public int Augmented
        {
            get
            {
                return Value + _intAugModifier;
            }
        }

        /// <summary>
        /// The total amount of the modifiers that affect the CharacterAttribute's value.
        /// </summary>
        public int AttributeModifiers
        {
            get
            {
                List<string> lstUniqueName = new List<string>();
                List<string[,]> lstUniquePair = new List<string[,]>();
                int intModifier = 0;
                foreach (Improvement objImprovement in _objCharacter.Improvements)
                {
                    if (objImprovement.Enabled && !objImprovement.Custom)
                    {
                        if (objImprovement.UniqueName != "" && objImprovement.ImproveType == Improvement.ImprovementType.Attribute && objImprovement.ImprovedName == _strAbbrev)
                        {
                            // If this has a UniqueName, run through the current list of UniqueNames seen. If it is not already in the list, add it.
                            bool blnFound = false;
                            foreach (string strName in lstUniqueName)
                            {
                                if (strName == objImprovement.UniqueName)
                                    blnFound = true;
                                break;
                            }
                            if (!blnFound)
                                lstUniqueName.Add(objImprovement.UniqueName);

                            // Add the values to the UniquePair List so we can check them later.
                            string[,] strValues = new string[,] { { objImprovement.UniqueName, (objImprovement.Augmented * objImprovement.Rating).ToString() } };
                            lstUniquePair.Add(strValues);
                        }
                        else
                        {
                            if (objImprovement.ImproveType == Improvement.ImprovementType.Attribute && objImprovement.ImprovedName == _strAbbrev)
                                intModifier += objImprovement.Augmented * objImprovement.Rating;
                        }
                    }
                }

                if (lstUniqueName.Contains("precedence0"))
                {
                    // Retrieve only the highest precedence0 value.
                    // Run through the list of UniqueNames and pick out the highest value for each one.
                    int intHighest = -999;
                    foreach (string[,] strValues in lstUniquePair)
                    {
                        if (strValues[0, 0] == "precedence0")
                        {
                            if (Convert.ToInt32(strValues[0, 1]) > intHighest)
                                intHighest = Convert.ToInt32(strValues[0, 1]);
                        }
                    }
                    if (lstUniqueName.Contains("precedence-1"))
                    {
                        foreach (string[,] strValues in lstUniquePair)
                        {
                            if (strValues[0, 0] == "precedence-1")
                            {
                                intHighest += Convert.ToInt32(strValues[0, 1]);
                            }
                        }
                    }
                    intModifier = intHighest;
                }
                else if (lstUniqueName.Contains("precedence1"))
                {
                    // Retrieve all of the items that are precedence1 and nothing else.
                    intModifier = 0;
                    foreach (string[,] strValues in lstUniquePair)
                    {
                        if (strValues[0, 0] == "precedence1" || strValues[0, 0] == "precedence-1")
                            intModifier += Convert.ToInt32(strValues[0, 1]);
                    }
                }
                else
                {
                    // Run through the list of UniqueNames and pick out the highest value for each one.
                    foreach (string strName in lstUniqueName)
                    {
                        int intHighest = -999;
                        foreach (string[,] strValues in lstUniquePair)
                        {
                            if (strValues[0, 0] == strName)
                            {
                                if (Convert.ToInt32(strValues[0, 1]) > intHighest)
                                    intHighest = Convert.ToInt32(strValues[0, 1]);
                            }
                        }
                        intModifier += intHighest;
                    }
                }

                // Factor in Custom Improvements.
                lstUniqueName = new List<string>();
                lstUniquePair = new List<string[,]>();
                int intCustomModifier = 0;
                if (_strAbbrev == "REA")
                {
                }
                foreach (Improvement objImprovement in _objCharacter.Improvements)
                {
                    if (objImprovement.Enabled && objImprovement.Custom)
                    {
                        if (objImprovement.UniqueName != "" && objImprovement.ImproveType == Improvement.ImprovementType.Attribute && objImprovement.ImprovedName == _strAbbrev)
                        {
                            // If this has a UniqueName, run through the current list of UniqueNames seen. If it is not already in the list, add it.
                            bool blnFound = false;
                            foreach (string strName in lstUniqueName)
                            {
                                if (strName == objImprovement.UniqueName)
                                    blnFound = true;
                                break;
                            }
                            if (!blnFound)
                                lstUniqueName.Add(objImprovement.UniqueName);

                            // Add the values to the UniquePair List so we can check them later.
                            string[,] strValues = new string[,] { { objImprovement.UniqueName, (objImprovement.Augmented * objImprovement.Rating).ToString() } };
                            lstUniquePair.Add(strValues);
                        }
                        else
                        {
                            if (objImprovement.ImproveType == Improvement.ImprovementType.Attribute && objImprovement.ImprovedName == _strAbbrev)
                                intCustomModifier += objImprovement.Augmented * objImprovement.Rating;
                        }
                    }
                }

                // Run through the list of UniqueNames and pick out the highest value for each one.
                foreach (string strName in lstUniqueName)
                {
                    int intHighest = -999;
                    foreach (string[,] strValues in lstUniquePair)
                    {
                        if (strValues[0, 0] == strName)
                        {
                            if (Convert.ToInt32(strValues[0, 1]) > intHighest)
                                intHighest = Convert.ToInt32(strValues[0, 1]);
                        }
                    }
                    intCustomModifier += intHighest;
                }

                intModifier += AttributeValueModifiers + intCustomModifier;
                return intModifier;
            }
        }

        /// <summary>
        /// The total amount of the modifiers that raise the actual value of the CharacterAttribute and increase its Karma cost.
        /// </summary>
        public int AttributeValueModifiers
        {
            get
            {
                List<string> lstUniqueName = new List<string>();
                List<string[,]> lstUniquePair = new List<string[,]>();
                int intModifier = 0;
                foreach (Improvement objImprovement in _objCharacter.Improvements)
                {
                    if (objImprovement.Enabled)
                    {
                        if (objImprovement.UniqueName != "" && objImprovement.ImproveType == Improvement.ImprovementType.Attribute && objImprovement.ImprovedName == _strAbbrev + "Base")
                        {
                            // If this has a UniqueName, run through the current list of UniqueNames seen. If it is not already in the list, add it.
                            bool blnFound = false;
                            foreach (string strName in lstUniqueName)
                            {
                                if (strName == objImprovement.UniqueName)
                                    blnFound = true;
                                break;
                            }
                            if (!blnFound)
                                lstUniqueName.Add(objImprovement.UniqueName);

                            // Add the values to the UniquePair List so we can check them later.
                            string[,] strValues = new string[,] { { objImprovement.UniqueName, (objImprovement.Augmented * objImprovement.Rating).ToString() } };
                            lstUniquePair.Add(strValues);
                        }
                        else
                        {
                            if (objImprovement.ImproveType == Improvement.ImprovementType.Attribute && objImprovement.ImprovedName == _strAbbrev + "Base")
                                intModifier += objImprovement.Augmented * objImprovement.Rating;
                        }
                    }
                }

                if (lstUniqueName.Contains("precedence0"))
                {
                    // Retrieve only the highest precedence0 value.
                    // Run through the list of UniqueNames and pick out the highest value for each one.
                    int intHighest = -999;
                    foreach (string[,] strValues in lstUniquePair)
                    {
                        if (strValues[0, 0] == "precedence0")
                        {
                            if (Convert.ToInt32(strValues[0, 1]) > intHighest)
                                intHighest = Convert.ToInt32(strValues[0, 1]);
                        }
                    }
                    if (lstUniqueName.Contains("precedence-1"))
                    {
                        foreach (string[,] strValues in lstUniquePair)
                        {
                            if (strValues[0, 0] == "precedence-1")
                            {
                                intHighest += Convert.ToInt32(strValues[0, 1]);
                            }
                        }
                    }
                    intModifier = intHighest;
                }
                else if (lstUniqueName.Contains("precedence1"))
                {
                    // Retrieve all of the items that are precedence1 and nothing else.
                    intModifier = 0;
                    foreach (string[,] strValues in lstUniquePair)
                    {
                        if (strValues[0, 0] == "precedence1" || strValues[0, 0] == "precedence-1")
                            intModifier += Convert.ToInt32(strValues[0, 1]);
                    }
                }
                else
                {
                    // Run through the list of UniqueNames and pick out the highest value for each one.
                    foreach (string strName in lstUniqueName)
                    {
                        int intHighest = -999;
                        foreach (string[,] strValues in lstUniquePair)
                        {
                            if (strValues[0, 0] == strName)
                            {
                                if (Convert.ToInt32(strValues[0, 1]) > intHighest)
                                    intHighest = Convert.ToInt32(strValues[0, 1]);
                            }
                        }
                        intModifier += intHighest;
                    }
                }

                return intModifier;
            }
        }

        /// <summary>
        /// Whether or not the CharacterAttribute has any modifiers from Improvements.
        /// </summary>
        public bool HasModifiers
        {
            get
            {
                foreach (Improvement objImprovement in _objCharacter.Improvements)
                {
                    if (objImprovement.ImproveType == Improvement.ImprovementType.Attribute && (objImprovement.ImprovedName == _strAbbrev || objImprovement.ImprovedName == _strAbbrev + "Base") && objImprovement.Enabled && objImprovement.Augmented != 0)
                        return true;
                }

                // If this is AGI or STR, factor in any Cyberlimbs.
                if (!_objCharacter.Options.DontUseCyberlimbCalculation && (_strAbbrev == "AGI" || _strAbbrev == "STR"))
                {
                    foreach (Cyberware objCyberware in _objCharacter.Cyberware)
                    {
                        if (objCyberware.Category == "Cyberlimb" && objCyberware.LimbSlot != "")
                            return true;
                    }
                }

                if ((_objCharacter.MAGEnabled || _objCharacter.RESEnabled) && _objCharacter.EssencePenalty > 0)
                {
                    return true;
                }
                return false;
            }
        }

        /// <summary>
        /// The total amount of the modifiers that affect the CharacterAttribute's Minimum value.
        /// </summary>
        public int MinimumModifiers
        {
            get
            {
                int intModifier = 0;
                foreach (Improvement objImprovement in _objCharacter.Improvements)
                {
                    if (objImprovement.ImproveType == Improvement.ImprovementType.Attribute && objImprovement.ImprovedName == _strAbbrev && objImprovement.Enabled)
                    {
                        intModifier += objImprovement.Minimum * objImprovement.Rating;
                    }
                }
                return intModifier;
            }
        }

        /// <summary>
        /// The total amount of the modifiers that affect the CharacterAttribute's Maximum value.
        /// </summary>
        public int MaximumModifiers
        {
            get
            {
                int intModifier = 0;
                foreach (Improvement objImprovement in _objCharacter.Improvements)
                {
                    if (objImprovement.ImproveType == Improvement.ImprovementType.Attribute && objImprovement.ImprovedName == _strAbbrev && objImprovement.Enabled)
                    {
                        intModifier += objImprovement.Maximum * objImprovement.Rating;
                    }
                }
                return intModifier;
            }
        }

        /// <summary>
        /// The total amount of the modifiers that affect the CharacterAttribute's Augmented Maximum value.
        /// </summary>
        public int AugmentedMaximumModifiers
        {
            get
            {
                int intModifier = 0;
                foreach (Improvement objImprovement in _objCharacter.Improvements)
                {
                    if (objImprovement.ImproveType == Improvement.ImprovementType.Attribute && objImprovement.ImprovedName == _strAbbrev && objImprovement.Enabled)
                    {
                        intModifier += objImprovement.AugmentedMaximum * objImprovement.Rating;
                    }
                }
                return intModifier;
            }
        }
		/// <summary>
		/// The CharacterAttribute's total value (Value + Modifiers). 
		/// </summary>
		public int CalculatedTotalValue(bool blnIncludeCyberlimbs = true)
		{
			int intMeat = Value + AttributeModifiers;
			int intReturn = intMeat;

			//// If this is AGI or STR, factor in any Cyberlimbs.
			if ((_strAbbrev == "AGI" || _strAbbrev == "STR") && !_objCharacter.Options.DontUseCyberlimbCalculation && blnIncludeCyberlimbs)
			{
				int intLimbTotal = 0;
				int intLimbCount = 0;
				foreach (Cyberware objCyberware in _objCharacter.Cyberware
					.Where(objCyberware => objCyberware.Category == "Cyberlimb")
					.Where(objCyberware => !string.IsNullOrWhiteSpace(objCyberware.LimbSlot) && !_objCharacter.Options.ExcludeLimbSlot.Contains(objCyberware.LimbSlot)))
				{
					intLimbCount++;
					switch (_strAbbrev)
					{
						case "STR":
							intLimbTotal += objCyberware.TotalStrength;
							break;
						default:
							intLimbTotal += objCyberware.TotalAgility;
							break;
					}
				}

				if (intLimbCount > 0)
				{
					intReturn = 0;
					if (intLimbCount < _objCharacter.Options.LimbCount)
					{
						// Not all of the limbs have been replaced, so we need to place the Attribute in the other "limbs" to get the average value.
						for (int i = intLimbCount + 1; i <= _objCharacter.Options.LimbCount; i++)
							intLimbTotal += intMeat;
						intLimbCount = _objCharacter.Options.LimbCount;
					}
					int intTotal = Convert.ToInt32(Math.Floor(Convert.ToDecimal((intLimbTotal), GlobalOptions.CultureInfo) / Convert.ToDecimal(intLimbCount, GlobalOptions.CultureInfo)));
					intReturn += intTotal;
				}
			}
		    if ((_strAbbrev == "RES" || _strAbbrev == "MAG" || _strAbbrev == "DEP"))
		    {
		        intReturn -= _objCharacter.EssencePenalty;
		    }
			// Do not let the CharacterAttribute go above the Metatype's Augmented Maximum.
			if (intReturn > TotalAugmentedMaximum)
				intReturn = TotalAugmentedMaximum;

			// An Attribute cannot go below 1 unless it is EDG, MAG, or RES, the character is a Critter, or the Metatype Maximum is 0.
			if (_objCharacter.CritterEnabled || _strAbbrev == "EDG" || _intMetatypeMax == 0 || (_objCharacter.EssencePenalty != 0 && (_strAbbrev == "MAG" || _strAbbrev == "RES")) || (_objCharacter.MetatypeCategory != "A.I." && _strAbbrev == "DEP"))
			{
				if (intReturn < 0)
					return 0;
			}
			else
			{
				if (intReturn < 1)
					return 1;
			}

			// If we're looking at MAG and the character is a Cyberzombie, MAG is always 1, regardless of ESS penalties and bonuses.
			if (_objCharacter.MetatypeCategory == "Cyberzombie" && _strAbbrev == "MAG")
				return 1;

			return intReturn;
		}

		/// <summary>
		/// The CharacterAttribute's total value (Value + Modifiers).
		/// </summary>
		public int TotalValue
		{
			get { return CalculatedTotalValue(); }
		}

		/// <summary>
		/// The CharacterAttribute's combined Minimum value (Metatype Minimum + Modifiers).
		/// </summary>
		public int TotalMinimum
        {
            get
            {
                int intReturn = MetatypeMinimum + MinimumModifiers;
                if (_objCharacter.IsCritter || _intMetatypeMax == 0)
                {
                    if (intReturn < 0)
                        intReturn = 0;
                }
                else
                {
                    if (intReturn < 1)
                        intReturn = 1;
                }
                /*
				if	(
					(_strAbbrev == "MAG" && !(_objCharacter.AdeptEnabled || _objCharacter.MagicianEnabled)) || 
					(_strAbbrev == "RES" && !_objCharacter.TechnomancerEnabled) || 
					(_strAbbrev == "DEP" && !(_objCharacter.Metatype == "A.I."))
					)
				{
					intReturn = 0;
				}*/

                if (_objCharacter.EssencePenalty != 0 && (_strAbbrev == "MAG" || _strAbbrev == "RES"))
                {
                    if (_objCharacter.Options.ESSLossReducesMaximumOnly || _objCharacter.OverrideSpecialAttributeEssenceLoss)
                    {
                        // If the House Rule for Essence Loss Only Affects Maximum MAG/RES is turned on, the minimum should always be 1 unless the total ESS penalty is greater than or equal to
                        // the CharacterAttribute's total maximum, in which case the minimum becomes 0.
                        if (_objCharacter.EssencePenalty >= _objCharacter.MAG.TotalMaximum)
                            intReturn = 0;
                        else
                            intReturn = 1;
                    }
                    else
                        intReturn = Math.Max(_intMetatypeMin - _objCharacter.EssencePenalty, 0);
                }

                // If we're looking at MAG and the character is a Cyberzombie, MAG is always 1, regardless of ESS penalties and bonuses.
                if (_objCharacter.MetatypeCategory == "Cyberzombie" && _strAbbrev == "MAG")
                    intReturn = 1;

                return intReturn;
            }
        }

        /// <summary>
        /// The CharacterAttribute's combined Maximum value (Metatype Maximum + Modifiers).
        /// </summary>
        public int TotalMaximum
        {
            get
            {
                int intReturn = MetatypeMaximum + MaximumModifiers;

                if (intReturn < 0)
                    intReturn = 0;

                // If we're looking at MAG and the character is a Cyberzombie, MAG is always 1, regardless of ESS penalties and bonuses.
                if (_objCharacter.MetatypeCategory == "Cyberzombie" && _strAbbrev == "MAG")
                    intReturn = 1;

                return intReturn;
            }
        }

        /// <summary>
        /// The CharacterAttribute's combined Augmented Maximum value (Metatype Augmented Maximum + Modifiers).
        /// </summary>
        public int TotalAugmentedMaximum
        {
            get
            {
                int intReturn = 0;
                if (_strAbbrev == "EDG" || _strAbbrev == "MAG" || _strAbbrev == "RES")
                    intReturn = TotalMaximum + AugmentedMaximumModifiers;
                else
                    intReturn = TotalMaximum + 4 + AugmentedMaximumModifiers;
                // intReturn = TotalMaximum + Convert.ToInt32(Math.Floor((Convert.ToDecimal(TotalMaximum, GlobalOptions.Instance.CultureInfo) / 2))) + AugmentedMaximumModifiers;

                if (intReturn < 0)
                    intReturn = 0;

                // If we're looking at MAG and the character is a Cyberzombie, MAG is always 1, regardless of ESS penalties and bonuses.
                if (_objCharacter.MetatypeCategory == "Cyberzombie" && _strAbbrev == "MAG")
                    intReturn = 1;

                return intReturn;
            }
        }

        /// <summary>
        /// CharacterAttribute abbreviation.
        /// </summary>
        public string Abbrev
        {
            get
            {
                return _strAbbrev;
            }
        }

		public string DisplayNameShort
        {
			get {
                if (string.IsNullOrWhiteSpace(_strDisplayNameShort))
                {
                    string strName = "String_Attribute{0}Short".Replace("{0}", _strAbbrev);
                    _strDisplayNameShort = LanguageManager.Instance.GetString(strName);
                    return _strDisplayNameShort;
                }
                return _strDisplayNameShort;
				}
            set { _strDisplayNameShort = value; }
        }

        public string DisplayNameLong
        {
            get
            {
				//TODO: Is this a terrible idea?
                if (string.IsNullOrWhiteSpace(_strDisplayNameLong))
                {
                    string strName = "String_Attribute{0}Long".Replace("{0}", _strAbbrev);
                    _strDisplayNameLong = LanguageManager.Instance.GetString(strName);
                    return _strDisplayNameLong;
                }
                return _strDisplayNameLong;
            }
            set { _strDisplayNameLong = value; }
        }

		public string DisplayNameFormatted
        {
            get
            {
                //TODO: Is this a terrible idea?
                if (string.IsNullOrWhiteSpace(_strDisplayNameFormatted))
                {
                    _strDisplayNameFormatted = DisplayNameLong + " (" + DisplayNameShort + ")";
                    return _strDisplayNameFormatted;
                }
                return _strDisplayNameFormatted;
            }
            set { _strDisplayNameFormatted = value; }
        }

        /// <summary>
        /// Is it possible to place points in Base or is it prevented? (Build method or skill group)
        /// </summary>
        public bool BaseUnlocked
        {
            get
            {
                return _objCharacter.BuildMethod.HaveSkillPoints();
            }
        }

        /// <summary>
        /// CharacterAttribute Limits
        /// </summary>
        public string MetatypeLimits
        {
            get
            {
                return string.Format("{0} / {1} ({2})", MetatypeMinimum, MetatypeMaximum, MetatypeAugmentedMaximum);
            }
        }

        /// <summary>
        /// CharacterAttribute Limits
        /// </summary>
        public string AugmentedMetatypeLimits
        {
            get
            {
                return string.Format("{0} / {1} ({2})", TotalMinimum, TotalMaximum, TotalAugmentedMaximum);
            }
        }
        #endregion

        #region Methods
        /// <summary>
        /// Set the minimum, maximum, and augmented values for the CharacterAttribute based on string values from the Metatype XML file.
        /// </summary>
        /// <param name="strMin">Metatype's minimum value for the CharacterAttribute.</param>
        /// <param name="strMax">Metatype's maximum value for the CharacterAttribute.</param>
        /// <param name="strAug">Metatype's maximum augmented value for the CharacterAttribute.</param>
        public void AssignLimits(string strMin, string strMax, string strAug)
        {
            MetatypeMinimum = Convert.ToInt32(strMin);
            MetatypeMaximum = Convert.ToInt32(strMax);
            MetatypeAugmentedMaximum = Convert.ToInt32(strAug);
        }

        /// <summary>
        /// ToolTip that shows how the CharacterAttribute is calculating its Modified Rating.
        /// </summary>
        public string ToolTip
        {
	        get
	        {
		        string strReturn = "";
		        strReturn += _strAbbrev + " (" + Value.ToString() + ")";
		        string strModifier = "";

		        List<string> lstUniqueName = new List<string>();
		        List<string[,]> lstUniquePair = new List<string[,]>();
		        foreach (Improvement objImprovement in _objCharacter.Improvements)
		        {
			        if (objImprovement.Enabled && !objImprovement.Custom)
			        {
				        if (objImprovement.UniqueName != "" && objImprovement.UniqueName != "enableattribute" && objImprovement.ImproveType == Improvement.ImprovementType.Attribute &&
				            objImprovement.ImprovedName == _strAbbrev)
				        {
					        // If this has a UniqueName, run through the current list of UniqueNames seen. If it is not already in the list, add it.
					        bool blnFound = false;
					        foreach (string strName in lstUniqueName)
					        {
						        if (strName == objImprovement.UniqueName)
							        blnFound = true;
						        break;
					        }
					        if (!blnFound)
						        lstUniqueName.Add(objImprovement.UniqueName);

					        // Add the values to the UniquePair List so we can check them later.
					        string[,] strValues = new string[,]
					        {
						        {
							        objImprovement.UniqueName, (objImprovement.Augmented * objImprovement.Rating).ToString(),
							        _objCharacter.GetObjectName(objImprovement)
						        }
					        };
					        lstUniquePair.Add(strValues);
				        }
				        else
				        {
					        if (objImprovement.ImproveType == Improvement.ImprovementType.Attribute &&
					            objImprovement.ImprovedName == _strAbbrev &&
					            !(objImprovement.Value == 0 && objImprovement.Augmented == 0))
						        strModifier += " + " + _objCharacter.GetObjectName(objImprovement) + " (" +
						                       (objImprovement.Augmented * objImprovement.Rating).ToString() + ")";
				        }
			        }
		        }

		        if (lstUniqueName.Contains("precedence0"))
		        {
			        // Retrieve only the highest precedence0 value.
			        // Run through the list of UniqueNames and pick out the highest value for each one.
			        int intHighest = -999;

			        foreach (string[,] strValues in lstUniquePair)
			        {
				        if (strValues[0, 0] == "precedence0")
				        {
					        if (Convert.ToInt32(strValues[0, 1]) > intHighest)
					        {
						        intHighest = Convert.ToInt32(strValues[0, 1]);
						        strModifier = " + " + strValues[0, 2] + " (" + strValues[0, 1] + ")";
					        }
				        }
			        }
			        if (lstUniqueName.Contains("precedence-1"))
			        {
				        foreach (string[,] strValues in lstUniquePair)
				        {
					        if (strValues[0, 0] == "precedence-1")
					        {
						        intHighest += Convert.ToInt32(strValues[0, 1]);
						        strModifier += " + " + strValues[0, 2] + " (" + strValues[0, 1] + ")";
					        }
				        }
			        }
		        }
		        else if (lstUniqueName.Contains("precedence1"))
		        {
			        // Retrieve all of the items that are precedence1 and nothing else.
			        strModifier = "";
			        foreach (string[,] strValues in lstUniquePair)
			        {
				        if (strValues[0, 0] == "precedence1" || strValues[0, 0] == "precedence-1")
					        strModifier += " + " + strValues[0, 2] + " (" + strValues[0, 1] + ")";
			        }
		        }
		        else
		        {
			        // Run through the list of UniqueNames and pick out the highest value for each one.
			        foreach (string strName in lstUniqueName)
			        {
				        int intHighest = -999;
				        foreach (string[,] strValues in lstUniquePair)
				        {
					        if (strValues[0, 0] == strName)
					        {
						        if (Convert.ToInt32(strValues[0, 1]) > intHighest)
						        {
							        intHighest = Convert.ToInt32(strValues[0, 1]);
							        strModifier += " + " + strValues[0, 2] + " (" + strValues[0, 1] + ")";
						        }
					        }
				        }
			        }
		        }

		        // Factor in Custom Improvements.
		        lstUniqueName = new List<string>();
		        lstUniquePair = new List<string[,]>();
		        foreach (Improvement objImprovement in _objCharacter.Improvements)
		        {
			        if (objImprovement.Enabled && objImprovement.Custom)
			        {
				        if (objImprovement.UniqueName != "" && objImprovement.ImproveType == Improvement.ImprovementType.Attribute &&
				            objImprovement.ImprovedName == _strAbbrev)
				        {
					        // If this has a UniqueName, run through the current list of UniqueNames seen. If it is not already in the list, add it.
					        bool blnFound = false;
					        foreach (string strName in lstUniqueName)
					        {
						        if (strName == objImprovement.UniqueName)
							        blnFound = true;
						        break;
					        }
					        if (!blnFound)
						        lstUniqueName.Add(objImprovement.UniqueName);

					        // Add the values to the UniquePair List so we can check them later.
					        string[,] strValues = new string[,]
					        {
						        {
							        objImprovement.UniqueName, (objImprovement.Augmented * objImprovement.Rating).ToString(),
							        _objCharacter.GetObjectName(objImprovement)
						        }
					        };
					        lstUniquePair.Add(strValues);
				        }
				        else
				        {
					        if (objImprovement.ImproveType == Improvement.ImprovementType.Attribute &&
					            objImprovement.ImprovedName == _strAbbrev)
						        strModifier += " + " + _objCharacter.GetObjectName(objImprovement) + " (" +
						                       (objImprovement.Augmented * objImprovement.Rating).ToString() + ")";
				        }
			        }
		        }

		        // Run through the list of UniqueNames and pick out the highest value for each one.
		        foreach (string strName in lstUniqueName)
		        {
			        int intHighest = -999;
			        foreach (string[,] strValues in lstUniquePair)
			        {
				        if (strValues[0, 0] == strName)
				        {
					        if (Convert.ToInt32(strValues[0, 1]) > intHighest)
					        {
						        intHighest = Convert.ToInt32(strValues[0, 1]);
						        strModifier = " + " + strValues[0, 2] + " (" + strValues[0, 1] + ")";
					        }
				        }
			        }
		        }

		        //// If this is AGI or STR, factor in any Cyberlimbs.
		        StringBuilder strCyberlimb = new StringBuilder();
		        if ((_strAbbrev == "AGI" || _strAbbrev == "STR") && !_objCharacter.Options.DontUseCyberlimbCalculation)
		        {
			        LanguageManager.Instance.Load(GlobalOptions.Instance.Language, null);
			        foreach (Cyberware objCyberware in _objCharacter.Cyberware)
			        {
				        if (objCyberware.Category == "Cyberlimb")
				        {
					        if (_strAbbrev == "AGI")
					        {
						        strCyberlimb.Append("\n");
						        strCyberlimb.Append(objCyberware.DisplayName + " (");
						        strCyberlimb.Append(objCyberware.TotalAgility.ToString());
						        strCyberlimb.Append(")");
					        }
					        else
					        {
						        strCyberlimb.Append("\n");
						        strCyberlimb.Append(objCyberware.DisplayName + " (");
						        strCyberlimb.Append(objCyberware.TotalStrength.ToString());
						        strCyberlimb.Append(")");
					        }
				        }
			        }
			        strModifier += strCyberlimb;
		        }

                if ((_strAbbrev == "RES" || _strAbbrev == "MAG" || _strAbbrev == "DEP") && _objCharacter.EssencePenalty != 0)
                {
                    strModifier += $" + -{_objCharacter.EssencePenalty} ({LanguageManager.Instance.GetString("String_AttributeESSLong")})";
                }

                return strReturn + strModifier;
	        }
        }

        /// <summary>
        /// Amount of BP/Karma spent on this CharacterAttribute.
        /// </summary>
        public int CalculatedBP()
        {
            int intBP = 0;

            if (_strAbbrev != "EDG" && _strAbbrev != "MAG" && _strAbbrev != "RES")
            {
                if (_objCharacter.Options.AlternateMetatypeAttributeKarma)
                {
                    // Weird house rule method that treats the Metatype's minimum as being 1 for the purpose of calculating Karma costs.
                    for (int i = 1; i <= _objCharacter.GetAttribute(_strAbbrev).Value - _objCharacter.GetAttribute(_strAbbrev).TotalMinimum; i++)
                        intBP += (i + 1) * _objCharacter.Options.KarmaAttribute;
                }
                else
                {
                    // Karma calculation starts from the minimum score + 1 and steps through each up to the current score. At each step, the current number is multplied by the Karma Cost to
                    // give us the cost of at each step.
                    for (int i = _objCharacter.GetAttribute(_strAbbrev).TotalMinimum + 1; i <= _objCharacter.GetAttribute(_strAbbrev).Value; i++)
                        intBP += i * _objCharacter.Options.KarmaAttribute;
                }
            }
            else
            {
                // Find the character's Essence Loss. This applies unless the house rule to have ESS Loss only affect the Maximum of the CharacterAttribute is turned on.
                int intEssenceLoss = 0;
                if (!_objCharacter.Options.ESSLossReducesMaximumOnly && !_objCharacter.OverrideSpecialAttributeEssenceLoss)
                    intEssenceLoss = _objCharacter.EssencePenalty;

                // Don't apply the ESS loss penalty to EDG.
                int intUseEssenceLoss = intEssenceLoss;
                if (_strAbbrev == "EDG")
                    intUseEssenceLoss = 0;

                // If the character has an ESS penalty, the minimum needs to be bumped up by 1 so that the cost calculation is correct.
                int intMinModifier = 0;
                if (intUseEssenceLoss > 0)
                    intMinModifier = 1;

                if (_objCharacter.GetAttribute(_strAbbrev).TotalMinimum == 0 && _objCharacter.GetAttribute(_strAbbrev).TotalMaximum == 0)
                {
                    intBP += 0;
                }
                else
                {
                    // Karma calculation starts from the minimum score + 1 and steps through each up to the current score. At each step, the current number is multplied by the Karma Cost to
                    // give us the cost of at each step.
                    for (int i = _objCharacter.GetAttribute(_strAbbrev).TotalMinimum + 1 + intMinModifier; i <= _objCharacter.GetAttribute(_strAbbrev).Value + intUseEssenceLoss; i++)
                        intBP += i * _objCharacter.Options.KarmaAttribute;
                }
            }

            return intBP;
        }

	    public int SpentPriorityPoints => Math.Max(Base - TotalMinimum, 0);

	    public bool AtMetatypeMaximum => Value == TotalMaximum;

        public int KarmaMaximum => TotalMaximum - Base;
        public int PriorityMaximum => TotalMaximum - Karma;
        /// <summary>
        /// Karma price to upgrade. Returns negative if impossible
        /// </summary>
        /// <returns>Price in karma</returns>
        public virtual int UpgradeKarmaCost()
        {
            int upgrade;
            if (Value >= TotalMaximum)
            {
                upgrade = -1;
            }
            else if (Value == 0)
            {
                upgrade = _objCharacter.Options.KarmaAttribute;
            }
            else
            {
                upgrade = (Value + 1) * _objCharacter.Options.KarmaImproveActiveSkill;
            }
            if (_objCharacter.Options.AlternateMetatypeAttributeKarma)
                upgrade -= (_objCharacter.STR.MetatypeMinimum - 1) * _objCharacter.Options.KarmaAttribute;

            if (_strAbbrev == "STR" && _objCharacter.Cyberware.Find(x =>
                        x.Name == "Myostatin Inhibitor") != null)
            {
                upgrade -= 2;
            }

            return upgrade;

        }

	    public virtual int TotalKarmaCost()
	    {
		    int intCost = 0;
		    if (!_objCharacter.Options.AlternateMetatypeAttributeKarma)
		    {
			    for (int i = 1; i <= Karma; i++)
			    {
				    if (Abbrev == "STR" && _objCharacter.Cyberware.Find(x =>
					    x.Name == "Myostatin Inhibitor") != null)
				    {
					    intCost += ((Convert.ToInt32(Base) + i)*_objCharacter.Options.KarmaAttribute) - 2;
				    }
				    else
				    {
					    intCost += ((Convert.ToInt32(Base) + i)*_objCharacter.Options.KarmaAttribute);
				    }
			    }
		    }
		    else
		    {
				for (int i = 1; i <= Karma; i++)
				{
					if (Abbrev == "STR" && _objCharacter.Cyberware.Find(x =>
						x.Name == "Myostatin Inhibitor") != null)
					{
						intCost += (Convert.ToInt32(1 + i) * _objCharacter.Options.KarmaAttribute) - 2;
					}
					else
					{
						intCost += (Convert.ToInt32(1 + i) * _objCharacter.Options.KarmaAttribute);
					}
					
				}
			}
		    return intCost;
	    }

        public bool CanUpgradeCareer
        {
            get { return _objCharacter.Karma >= UpgradeKarmaCost() && TotalMaximum > Value; }
        }

		/// <summary>
		/// Check if any other CharacterAttribute is already at its Metatype Maximum.
		/// </summary>
		public bool CanImproveAttribute()
		{
			CharacterAttrib objAttribute = this;
			bool blnAtMaximum = false;
			if (_objCharacter.Created)
			{
				return CanUpgradeCareer;
			}
			if (_objCharacter.Options.Allow2ndMaxAttribute)
			{
				if (objAttribute.Abbrev != "STR")
				{
					if (((_objCharacter.STR.Base + _objCharacter.STR.Karma) == _objCharacter.STR.TotalMaximum) && _objCharacter.STR.TotalMaximum != 0)
						blnAtMaximum = true;
				}
				if (objAttribute.Abbrev != "BOD")
				{
					if (((_objCharacter.BOD.Base + _objCharacter.BOD.Karma) == _objCharacter.BOD.TotalMaximum) && _objCharacter.BOD.TotalMaximum != 0)
						blnAtMaximum = true;
				}
				if (objAttribute.Abbrev != "AGI")
				{
					if (((_objCharacter.AGI.Base + _objCharacter.AGI.Karma) == _objCharacter.AGI.TotalMaximum) && _objCharacter.AGI.TotalMaximum != 0)
						blnAtMaximum = true;
				}
				if (objAttribute.Abbrev != "REA")
				{
					if (((_objCharacter.REA.Base + _objCharacter.REA.Karma) == _objCharacter.REA.TotalMaximum) && _objCharacter.REA.TotalMaximum != 0)
						blnAtMaximum = true;
				}
				if (objAttribute.Abbrev != "CHA")
				{
					if (((_objCharacter.CHA.Base + _objCharacter.CHA.Karma) == _objCharacter.CHA.TotalMaximum) && _objCharacter.CHA.TotalMaximum != 0)
						blnAtMaximum = true;
				}
				if (objAttribute.Abbrev != "INT")
				{
					if (((_objCharacter.INT.Base + _objCharacter.INT.Karma) == _objCharacter.INT.TotalMaximum) && _objCharacter.INT.TotalMaximum != 0)
						blnAtMaximum = true;
				}
				if (objAttribute.Abbrev != "WIL")
				{
					if (((_objCharacter.WIL.Base + _objCharacter.WIL.Karma) == _objCharacter.WIL.TotalMaximum) && _objCharacter.WIL.TotalMaximum != 0)
						blnAtMaximum = true;
				}
				if (objAttribute.Abbrev != "LOG")
				{
					if (((_objCharacter.LOG.Base + _objCharacter.LOG.Karma) == _objCharacter.LOG.TotalMaximum) && _objCharacter.LOG.TotalMaximum != 0)
						blnAtMaximum = true;
				}
			}
			else
			{
				if (objAttribute.Abbrev != "STR")
				{
					if (((_objCharacter.STR.Base + _objCharacter.STR.Karma) == _objCharacter.STR.TotalMaximum) && _objCharacter.STR.TotalMaximum != 0)
						blnAtMaximum = true;
				}
				if (objAttribute.Abbrev != "BOD")
				{
					if (((_objCharacter.BOD.Base + _objCharacter.BOD.Karma) == _objCharacter.BOD.TotalMaximum) && _objCharacter.BOD.TotalMaximum != 0)
						blnAtMaximum = true;
				}
				if (objAttribute.Abbrev != "AGI")
				{
					if (((_objCharacter.AGI.Base + _objCharacter.AGI.Karma) == _objCharacter.AGI.TotalMaximum) && _objCharacter.AGI.TotalMaximum != 0)
						blnAtMaximum = true;
				}
				if (objAttribute.Abbrev != "REA")
				{
					if (((_objCharacter.REA.Base + _objCharacter.REA.Karma) == _objCharacter.REA.TotalMaximum) && _objCharacter.REA.TotalMaximum != 0)
						blnAtMaximum = true;
				}
				if (objAttribute.Abbrev != "CHA")
				{
					if (((_objCharacter.CHA.Base + _objCharacter.CHA.Karma) == _objCharacter.CHA.TotalMaximum) && _objCharacter.CHA.TotalMaximum != 0)
						blnAtMaximum = true;
				}
				if (objAttribute.Abbrev != "INT")
				{
					if (((_objCharacter.INT.Base + _objCharacter.INT.Karma) == _objCharacter.INT.TotalMaximum) && _objCharacter.INT.TotalMaximum != 0)
						blnAtMaximum = true;
				}
				if (objAttribute.Abbrev != "WIL")
				{
					if (((_objCharacter.WIL.Base + _objCharacter.WIL.Karma) == _objCharacter.WIL.TotalMaximum) && _objCharacter.WIL.TotalMaximum != 0)
						blnAtMaximum = true;
				}
				if (objAttribute.Abbrev != "LOG")
				{
					if (((_objCharacter.LOG.Base + _objCharacter.LOG.Karma) == _objCharacter.LOG.TotalMaximum) && _objCharacter.LOG.TotalMaximum != 0)
						blnAtMaximum = true;
				}
			}

			return !blnAtMaximum;
		}

		[NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

		}

		/// <summary>
		/// Convert a string to a LifestyleType.
		/// </summary>
		/// <param name="strValue">String value to convert.</param>
		/// <param name="strAbbrev">Linked attribute abbreviation.</param>
		public AttributeCategory ConvertToAttributeCategory(string strValue, string strAbbrev = "")
		{
			//If the value does not exist, figure out what it should be from the abbreviation.
			if (string.IsNullOrWhiteSpace(strValue))
			{
				switch (strAbbrev)
				{
					case "EDG":
					case "MAG":
					case "RES":
					case "DEP":
						return AttributeCategory.Special;
					default:
						return AttributeCategory.Standard;
				}
			}
			//If a value does exist, test whether it belongs to a shapeshifter form.
			switch (strValue)
			{
				case "Shapeshifter":
					return AttributeCategory.Shapeshifter;
				case "Special":
					return AttributeCategory.Special;
				default:
					return AttributeCategory.Standard;
			}
		}
		#endregion

		#region static

		private static readonly Lazy<HashSet<string>> _physicalAttributes =
            new Lazy<HashSet<string>>(() => new HashSet<string>() { "BOD", "AGI", "REA", "STR" },
                LazyThreadSafetyMode.PublicationOnly);
        private string _strDisplayNameShort;
        private string _strDisplayNameLong;
        private string _strDisplayNameFormatted;
	    private Enum _enumCategory;
		private string _strDisplayAbbrev;

		public static HashSet<string> PhysicalAttributes
        {
            get { return _physicalAttributes.Value; }
        }

        public string UpgradeKarmaCostString
        {
            get
            {
               return LanguageManager.Instance.GetString("Message_ConfirmKarmaExpense").Replace("{0}", _strAbbrev.Replace("{1}", (Value + AttributeValueModifiers + 1).ToString()).Replace("{2}", UpgradeKarmaCost().ToString()));
            }
        }

		/// <summary>
		/// Translated abbreviation of the attribute.
		/// </summary>
		public string DisplayAbbrev
		{
			get
			{
				if (string.IsNullOrWhiteSpace(_strDisplayAbbrev))
				{
					_strDisplayAbbrev = LanguageManager.Instance.GetString($"String_Attribute{Abbrev}Short");
				}
				return _strDisplayAbbrev;
			}
		}

		public void Upgrade()
        {
            if (!CanUpgradeCareer) return;

            int price = UpgradeKarmaCost();
            string upgradetext = LanguageManager.Instance.GetString("String_ExpenseAttribute") + " " + _strAbbrev + " " + (Value + AttributeValueModifiers).ToString() + " -> " + (Value + AttributeValueModifiers + 1).ToString();

            ExpenseLogEntry entry = new ExpenseLogEntry();
            entry.Create(price * -1, upgradetext, ExpenseType.Karma, DateTime.Now);
            entry.Undo = new ExpenseUndo().CreateKarma(KarmaExpenseType.ImproveAttribute, _strAbbrev);

            _objCharacter.ExpenseEntries.Add(entry);

            Karma += 1;
            _objCharacter.Karma -= price;
        }

	    public void Degrade(int intValue)
	    {
		    for (int i = intValue; i > 0; i--)
		    {
			    if (Karma > 0)
			    {
				    Karma -= 1;
			    }
			    else if (Base > 0)
			    {
				    Base -= 1;
			    }
			    else
				    return;
		    }
	    }
        #endregion
    }
}
