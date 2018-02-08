﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ARKBreedingStats
{
    public partial class CreatureBox : UserControl
    {
        private Creature creature;
        private StatDisplay[] stats;
        private NumericUpDown[] numUDLevelsDom;
        public delegate void ChangedEventHandler(Creature creature, bool creatureStatusChanged);
        public event ChangedEventHandler Changed;
        public delegate void EventHandler(object sender, Creature creature);
        public event EventHandler GiveParents;
        public event EventHandler EditCreature;
        private Sex sex;
        private CreatureStatus status;
        public List<Creature>[] parentList; // all creatures that could be parents (i.e. same species, separated by sex)
        public List<int>[] parentListSimilarity; // for all possible parents the number of equal stats (to find the parents easier)
        private MyColorPicker cp;
        private Button[] colorButtons;
        private List<ColorRegion> colorRegions;
        private bool[] colorRegionUseds;
        private Image largeImage;
        private bool renewLargeImage;
        public int maxDomLevel = 0;
        ToolTip tt = new ToolTip();

        public CreatureBox()
        {
            initializeVars();
        }

        public CreatureBox(Creature creature)
        {
            initializeVars();
            setCreature(creature);
        }

        private void initializeVars()
        {
            InitializeComponent();
            this.creature = null;
            stats = new StatDisplay[] { statDisplayHP, statDisplaySt, statDisplayOx, statDisplayFo, statDisplayWe, statDisplayDm, statDisplaySp, statDisplayTo };
            numUDLevelsDom = new NumericUpDown[] { numericUpDown1, numericUpDown2, numericUpDown3, numericUpDown4, numericUpDown5, numericUpDown6, numericUpDown7 };
            stats[0].Title = "HP";
            stats[1].Title = "St";
            stats[2].Title = "Ox";
            stats[3].Title = "Fo";
            stats[4].Title = "We";
            stats[5].Title = "Dm";
            stats[6].Title = "Sp";
            stats[7].Title = "To";
            stats[5].Percent = true;
            stats[6].Percent = true;
            statDisplayTo.ShowBars = false;
            colorButtons = new Button[] { buttonColor1, buttonColor2, buttonColor3, buttonColor4, buttonColor5, buttonColor6 };
            parentComboBoxMother.naLabel = "- Mother n/a";
            parentComboBoxFather.naLabel = "- Father n/a";

            // tooltips
            tt.SetToolTip(this.labelHeaderDomLevelSet, "Set the spend domesticated Levels here");
            tt.SetToolTip(labelGender, "Sex of the Creature");
            tt.SetToolTip(labelStatHeader, "Wild-levels, Domesticated-levels, Value that is inherited, Current Value of the Creature");
            tt.SetToolTip(buttonEdit, "Edit");
            tt.SetToolTip(labelM, "Mother");
            tt.SetToolTip(labelF, "Father");
            tt.SetToolTip(textBoxNote, "Note");
            tt.SetToolTip(labelParents, "Mother and Father (if bred and choosen)");
            tt.SetToolTip(buttonSex, "Sex");
            tt.SetToolTip(buttonStatus, "Status: Available, Unavailable, Dead");
            cp = new MyColorPicker();
        }

        public void setCreature(Creature creature)
        {
            Clear();
            this.creature = creature;
            int si = Values.V.speciesNames.IndexOf(creature.species);
            if (si >= 0 && Values.V.species[si].colors != null)
                colorRegions = Values.V.species[si].colors;
            else
            {
                colorRegions = new List<ColorRegion>();
                for (int i = 0; i < 6; i++)
                {
                    colorRegions.Add(new ColorRegion());
                    colorRegions[i].name = "n/a";
                }
            }
            colorRegionUseds = colorRegions.Select(c => c.name != null).ToArray();
            updateLabel();
            renewLargeImage = true;
        }

        public int BarMaxLevel
        {
            set
            {
                for (int s = 0; s < 8; s++)
                {
                    stats[s].barMaxLevel = value;
                }
            }
        }

        public void updateStat(int stat)
        {
            if (stat >= 0 && stat < 8)
            {
                stats[stat].setNumbers(creature.levelsWild[stat], creature.levelsDom[stat], creature.valuesBreeding[stat], creature.valuesDom[stat]);
            }
        }

        public void buttonEdit_Click(object sender, EventArgs e)
        {
            SuspendLayout();
            if (creature != null)
            {
                if (panel1.Visible)
                {
                    closeSettings(false);
                }
                else
                {
                    checkBoxIsBred.Checked = creature.isBred;
                    panelParents.Visible = creature.isBred;
                    if (creature.isBred)
                        populateParentsList();
                    textBoxName.Text = creature.name;
                    textBoxOwner.Text = creature.owner;
                    textBoxNote.Text = creature.note;
                    sex = creature.gender;
                    buttonSex.Text = Utils.sexSymbol(sex);
                    status = creature.status;
                    buttonStatus.Text = Utils.statusSymbol(status);
                    textBoxName.SelectAll();
                    textBoxName.Focus();
                    panel1.Visible = true;
                    for (int s = 0; s < 7; s++)
                    {
                        numUDLevelsDom[s].Value = creature.levelsDom[s];
                    }
                }
            }
            ResumeLayout();
        }

        private void populateParentsList()
        {
            if (parentList[0] == null || parentList[1] == null)
            {
                GiveParents(this, creature);

                parentComboBoxMother.preselectedCreatureGuid = creature.motherGuid;
                parentComboBoxFather.preselectedCreatureGuid = creature.fatherGuid;
                parentComboBoxMother.parentsSimilarity = parentListSimilarity[0];
                parentComboBoxMother.ParentList = parentList[0];
                parentComboBoxFather.parentsSimilarity = parentListSimilarity[1];
                parentComboBoxFather.ParentList = parentList[1];
            }
        }

        public void updateLabel()
        {
            if (creature != null)
            {
                labelGender.Text = Utils.sexSymbol(creature.gender);
                groupBox1.Text = creature.name + " (Lvl " + creature.level + "/" + (creature.levelHatched + maxDomLevel) + ")";
                if (creature.Mother != null || creature.Father != null)
                {
                    labelParents.Text = "";
                    if (creature.Mother != null)
                        labelParents.Text = "Mo: " + creature.Mother.name;
                    if (creature.Father != null && creature.Mother != null)
                        labelParents.Text += "; ";
                    if (creature.Father != null)
                        labelParents.Text += "Fa: " + creature.Father.name;
                }
                else if (creature.isBred)
                {
                    labelParents.Text = "bred, click 'edit' to add parents";
                }
                else
                {
                    labelParents.Text = "found wild " + creature.levelFound + (creature.tamingEff >= 0 ? ", tamed with TE: " + (creature.tamingEff * 100).ToString("N1") + "%" : ", TE unknown.");
                }
                for (int s = 0; s < 8; s++) { updateStat(s); }
                labelNotes.Text = creature.note;
                labelSpecies.Text = creature.species;
                pictureBox1.Image = CreatureColored.getColoredCreature(creature.colors, creature.species, colorRegionUseds);
                pictureBox1.Visible = true;

                for (int c = 0; c < 6; c++)
                {
                    if (colorRegionUseds[c])
                    {
                        setColorButton(colorButtons[c], Utils.creatureColor(creature.colors[c]));
                        tt.SetToolTip(colorButtons[c], colorRegions[c].name);
                        colorButtons[c].Visible = true;
                    }
                    else
                    {
                        colorButtons[c].Visible = false;
                    }
                }
            }
        }

        private void closeSettings(bool save)
        {
            panel1.Visible = false;
            if (save)
            {
                SuspendLayout();
                creature.name = textBoxName.Text;
                creature.gender = sex;
                creature.owner = textBoxOwner.Text;
                Creature parent = null;
                if (checkBoxIsBred.Checked)
                    parent = parentComboBoxMother.SelectedParent;
                creature.motherGuid = (parent != null ? parent.guid : Guid.Empty);
                bool parentsChanged = false;
                if (creature.Mother != parent)
                {
                    creature.Mother = parent;
                    parentsChanged = true;
                }
                parent = null;
                if (checkBoxIsBred.Checked)
                    parent = parentComboBoxFather.SelectedParent;
                creature.fatherGuid = (parent != null ? parent.guid : Guid.Empty);
                if (creature.Father != parent)
                {
                    creature.Father = parent;
                    parentsChanged = true;
                }
                if (parentsChanged)
                    creature.recalculateAncestorGenerations();

                creature.isBred = checkBoxIsBred.Checked;

                for (int s = 0; s < 7; s++)
                {
                    creature.levelsDom[s] = (int)numUDLevelsDom[s].Value;
                }
                creature.note = textBoxNote.Text;
                bool creatureStatusChanged = (creature.status != status);
                creature.status = status;

                Changed(creature, creatureStatusChanged);
                updateLabel();
                ResumeLayout();
            }
        }

        // call this function to clear all contents of this element
        public void Clear()
        {
            parentComboBoxMother.Items.Clear();
            parentComboBoxFather.Items.Clear();
            parentList = new List<Creature>[2];
            closeSettings(false);
            labelGender.Text = "";
            groupBox1.Text = "";
            creature = null;
            for (int s = 0; s < 8; s++)
            {
                stats[s].setNumbers(0, 0, 0, 0);
            }
            pictureBox1.Visible = false;
            for (int b = 0; b < 6; b++)
                colorButtons[b].Visible = false;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            closeSettings(true);
        }

        private void buttonCancel_Click(object sender, EventArgs e)
        {
            closeSettings(false);
        }

        private void buttonGender_Click(object sender, EventArgs e)
        {
            sex = Utils.nextSex(sex);
            buttonSex.Text = Utils.sexSymbol(sex);
        }

        private void buttonStatus_Click(object sender, EventArgs e)
        {
            status = Utils.nextStatus(status);
            buttonStatus.Text = Utils.statusSymbol(status);
        }

        private void checkBoxIsBred_CheckedChanged(object sender, EventArgs e)
        {
            panelParents.Visible = checkBoxIsBred.Checked;
            if (checkBoxIsBred.Checked)
                populateParentsList();
        }

        private void buttonColor1_Click(object sender, EventArgs e)
        {
            chooseColor(0, buttonColor1);
        }

        private void buttonColor2_Click(object sender, EventArgs e)
        {
            chooseColor(1, buttonColor2);
        }

        private void buttonColor3_Click(object sender, EventArgs e)
        {
            chooseColor(2, buttonColor3);
        }

        private void buttonColor4_Click(object sender, EventArgs e)
        {
            chooseColor(3, buttonColor4);
        }

        private void buttonColor5_Click(object sender, EventArgs e)
        {
            chooseColor(4, buttonColor5);
        }

        private void buttonColor6_Click(object sender, EventArgs e)
        {
            chooseColor(5, buttonColor6);
        }

        private void chooseColor(int region, Button sender)
        {
            if (creature != null && !cp.isShown)
            {
                cp.SetColors(creature.colors, region, colorRegions[region].name, colorRegions[region].colorIds);
                if (cp.ShowDialog() == DialogResult.OK)
                {
                    // color was chosen
                    setColorButton(sender, Utils.creatureColor(creature.colors[region]));
                    pictureBox1.Image = CreatureColored.getColoredCreature(creature.colors, creature.species, colorRegionUseds);
                    renewLargeImage = true;
                    Changed(creature, false);
                }
            }
        }

        private void setColorButton(Button bt, Color cl)
        {
            bt.BackColor = cl;
            bt.ForeColor = Utils.foreColor(cl);
        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {
            if (renewLargeImage)
            {
                largeImage = CreatureColored.getColoredCreature(creature.colors, creature.species, colorRegionUseds, 256);
                renewLargeImage = false;
            }
        }

        private void buttonEditMore_Click(object sender, EventArgs e)
        {
            EditCreature?.Invoke(this, creature);
        }
    }
}