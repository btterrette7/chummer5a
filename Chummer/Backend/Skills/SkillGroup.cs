﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using Chummer.Annotations;
using Chummer.Datastructures;

namespace Chummer.Skills
{
	public class SkillGroup : INotifyPropertyChanged
	{
		internal static SkillGroup Get(Skill skill)
		{
			if(skill.SkillGroupObject != null) return skill.SkillGroupObject;

			foreach (SkillGroup skillGroup in skill.CharacterObject.SkillGroups)
			{
				if (skillGroup._groupName == skill.SkillGroup)
				{
					if(!skillGroup._affectedSkills.Contains(skill))
						skillGroup.Add(skill);
					return skillGroup;

				}
			}

			if (string.IsNullOrWhiteSpace(skill.SkillGroup)) return null;

			SkillGroup newGroup = new SkillGroup(skill.CharacterObject, skill.SkillGroup);
			skill.CharacterObject.SkillGroups.Add(newGroup);
			newGroup.Add(skill);

			//BindingList don't have sort, so we have to play dirty
			List<SkillGroup> g = new List<SkillGroup>(skill.CharacterObject.SkillGroups.OrderBy(x => x.DisplayName));
			skill.CharacterObject.SkillGroups.Clear();
			foreach (SkillGroup skillGroup in g)
			{
				skill.CharacterObject.SkillGroups.Add(skillGroup);
			}

			return newGroup;
		}

		private void Add(Skill skill)
		{
			_affectedSkills.Add(skill);
			skill.PropertyChanged += SkillOnPropertyChanged;
		}

		private void SkillOnPropertyChanged(object sender, PropertyChangedEventArgs propertyChangedEventArgs)
		{
			if (propertyChangedEventArgs.PropertyName == nameof(Skill.Base))
			{
				if (_baseBrokenOldValue != BaseUnbroken)
					OnPropertyChanged(nameof(BaseUnbroken));

				_baseBrokenOldValue = BaseUnbroken;
			}

			if (propertyChangedEventArgs.PropertyName == nameof(Skill.Base) ||
			    propertyChangedEventArgs.PropertyName == nameof(Skill.Karma))
			{
				if (!KarmaUnbroken && _skillFromKarma > 0)
				{
					_skillFromKarma = 0;
					OnPropertyChanged(nameof(Karma));
				}

				if (_karmaBrokenOldValue != KarmaUnbroken) { 
					OnPropertyChanged(nameof(KarmaUnbroken));
}
				_karmaBrokenOldValue = KarmaUnbroken;
			}
		}

		private bool _baseBrokenOldValue;
		private bool _karmaBrokenOldValue;
		private List<Skill> _affectedSkills = new List<Skill>(); 
		private int _skillFromSp;
		private int _skillFromKarma;
		private readonly string _groupName;
		private readonly Character _character;
		private SkillGroup(Character character, string groupName)
		{
			_character = character;
			_groupName = groupName;
			_baseBrokenOldValue = BaseUnbroken;

			ImprovementEvent += OnImprovementEvent;
		}
		
		//TODO CALCULATIONS STUFF HERE
		public int Base
		{
			get
			{
				return _skillFromSp + FreeBase();

			}
			set
			{
				if (this.BaseUnbroken)
				{
					int max = 0;
					int old = _skillFromSp;

					//Calculate how far above maximum we are. 
					int overMax = (-1)*(RatingMaximum - (value + _skillFromKarma + FreeLevels()));
					
					//reduce value by max or 0
					//TODO karma from skill, karma other stuff might be reduced
					value -= Math.Max(0, overMax);

					//and save back, cannot go under 0
					_skillFromSp = Math.Max(0, value -  FreeBase());

					if(old != _skillFromSp) OnPropertyChanged();
				}
			}
		}

		public int Karma
		{
			get { return _skillFromKarma + FreeLevels(); }
			set
			{
				if (this.KarmaUnbroken)
				{
					int max = 0;
					int old = _skillFromKarma;

					//Calculate how far above maximum we are. 
					int overMax = (-1)*(RatingMaximum - (value + _skillFromSp + FreeBase()));

					//reduce value by max or 0
					//TODO can remove karma from skills
					value -= Math.Max(0, overMax);

					//and save back, cannot go under 0
					_skillFromKarma = Math.Max(0, value - FreeLevels());

					if (old != _skillFromKarma) OnPropertyChanged();
				}
			}
		}

		/// <summary>
		/// Is it possible to increment this skill group from points
		/// Inverted to simplifly databinding
		/// </summary>
		public bool BaseUnbroken
		{
			get
			{
				return _character.BuildMethod.HaveSkillPoints() && !_affectedSkills.Any(x => x.IBase > 0);
			}
		}

		/// <summary>
		/// Is it possible to increment this skill group from karma
		/// Inverted to simplifly databinding
		/// </summary>
		public bool KarmaUnbroken
		{
			get
			{
				int high = _affectedSkills.Max(x => x.IBase);
				bool ret = _affectedSkills.Any(x => x.IBase + x.IKarma < high);

				return !ret;
			}
		}

		public int Rating
		{
			get { return Karma + Base; }
		}

		internal int FreeBase()
		{
			return (from improvement in _character.Improvements
				   where improvement.ImproveType == Improvement.ImprovementType.SkillGroupBase
				      && improvement.ImprovedName == _groupName
				  select improvement.Value).Sum();	
		}

		int FreeLevels()
		{
			return (from improvement in _character.Improvements
				   where improvement.ImproveType == Improvement.ImprovementType.SkillGroupLevel
					  && improvement.ImprovedName == _groupName
				  select improvement.Value).Sum();
		}

		public int RatingMaximum
		{
			get
			{
				return (_character.Created ? 12 : 6);
			}
		}

		public Character Character
		{
			get { return _character; }
		}

		public string Name
		{
			get { return _groupName; }
		}

		public string DisplayName
		{
			get { return Name; } //TODO TRANSLATE
		}

		public bool HasCombatSkills
		{
			get { return _affectedSkills.Any(x => x.SkillCategory == "Combat Active"); }
		}

		public bool HasPhysicalSkills
		{
			get { return _affectedSkills.Any(x => x.SkillCategory == "Physical Active"); ; }
		}

		public bool HasSocialSkills
		{
			get { return _affectedSkills.Any(x => x.SkillCategory == "Social Active"); ; }
		}

		public bool HasTechnicalSkills
		{
			get { return _affectedSkills.Any(x => x.SkillCategory == "Technical Active"); ; }
		}

		public bool HasVehicleSkills
		{
			get { return _affectedSkills.Any(x => x.SkillCategory == "Vehicle Active"); ; }
		}

		public bool HasMagicalSkills
		{
			get { return _affectedSkills.Any(x => x.SkillCategory == "Magical Active"); ; }
		}

		public bool HasResonanceSkills
		{
			get { return _affectedSkills.Any(x => x.SkillCategory == "Resonance Active"); ; }
		}

		public event PropertyChangedEventHandler PropertyChanged;

		[NotifyPropertyChangedInvocator]
		protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}
		
		[Obsolete("Refactor this method away once improvementmanager gets outbound events")]
		private void OnImprovementEvent(List<Improvement> improvements, ImprovementManager improvementManager)
		{
			if (improvements.Any(imp => imp.ImproveType == Improvement.ImprovementType.SkillGroupLevel
			                            && imp.ImprovedName == _groupName))
			{
				OnPropertyChanged(nameof(FreeLevels));
				OnPropertyChanged(nameof(Base));
				//OnPropertyChanged(nameof(Base));
			}

		}

		//I also think this prevents GC. But there is no good way to do it short of rewriting improvements
		private static event Action<List<Improvement>, ImprovementManager> ImprovementEvent;
		//To get when things change in improvementmanager
		//Ugly, ugly done, but we cannot get events out of it today
		// FUTURE REFACTOR HERE
		[Obsolete("Refactor this method away once improvementmanager gets outbound events")]
		internal static void ImprovementHook(List<Improvement> _lstTransaction, ImprovementManager improvementManager)
		{
			ImprovementEvent?.Invoke(_lstTransaction, improvementManager);
		}

		[Obsolete("Only here as old code depends on it, remove in time")]
		public bool Broken { get; set; }
	}
}