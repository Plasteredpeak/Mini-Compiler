using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace slr
{


    class Rule
    {
        public string nonTerminal;
        public List<string> production = new List<string>();
        public bool isComplete = false;

        //constructor for Rule
        public Rule(string nt, List<string> p)
        {
            this.nonTerminal = nt;
            this.production = p;
        }
        //copy a rule in this format
        public Rule copy()
        {
            List<string> newp = new List<string>();
            newp = this.production.ToList();
            Rule r = new Rule(nonTerminal, newp);
            return r;
        }

        //convert string from hashtable into a rule by adding -> before nonterminal and all the terminals after it
        public string convertString()
        {
            string str = "";
            str += this.nonTerminal + "->";
            foreach(string p in this.production)
            {
                if(p != ".")
                {
                    str += p;
                }
            }
            return str;
        }


        //check if 2 rules match eachother
        public bool matches(Rule r)
        {
            bool matches = true;
            if(r.nonTerminal != this.nonTerminal)
            {
                matches = false;
            }
            else
            {
                if(r.production.Count != this.production.Count)
                {
                    matches = false;
                }
                else
                {
                    for (int i = 0; i < this.production.Count; i++)
                    {
                        if(r.production[i] != this.production[i])
                        {
                            matches = false;
                            break;
                        }
                    }
                }
            }
            return matches;
        }
    }

    class State
    {
        //name of state eg 0 1 2 3 etc
        public string name = "";
        //input from previous state
        public string input = "";
        //output production rules
        public Dictionary<string, State> outputs = new Dictionary<string, State>();
        //check if non terminal needs to be expanded
        public bool extended = false;
        //list of production rules
        public List<Rule> rules = new List<Rule>();
        public bool completeState = false;
        //accepting states or reducing sates
        public Dictionary<string, Rule> reductionRules = new Dictionary<string, Rule>();

        //constructor with only name
        public State(string name)
        {
            this.name = name;
        }
        //check if rule is complete then add it rules list
        public void addRule(Rule r)
        {
            if(r.production.Last() == ".")
            {
                r.isComplete = true;
            }
            rules.Add(r);  
        }
        //get final states which can be used for reductions
        public void getReductionRules(Hashtable followSets)
        {
            foreach(Rule rule in this.rules)
            {
                //check if rule is complete ie dot is at end
                if (rule.isComplete)
                {
                    //if a rule is complete find its follow set in hashtable and split its values by , and store it in a list
                    List<string> fsarray = cleanFollowSet(followSets[rule.nonTerminal].ToString()).Split(',').ToList();

                    //for each item in that follow set
                    foreach(string item in fsarray)
                    {
                        if(item != "" && item != " " && item != "," )
                        {
                            //reduction rule does not already have that item add it with the rule
                            if (!this.reductionRules.ContainsKey(item))
                            {
                                this.reductionRules.Add(item, rule);
                            }

                         
                        }
                    }
                }
            }
        }
        private string cleanFollowSet(string followset)
        {
            string finalFollowSet = "";
            String[] temparr = followset.Split(',');

            for (int i = 0; i < temparr.Length; i++)
            {
                string item = temparr[i];
                if (item != " " && item != "" && !finalFollowSet.Contains(item))
                {
                    if (i != 0)
                    {
                        finalFollowSet += ",";
                    }
                    finalFollowSet += item;
                }
            }
            return finalFollowSet;
        }

        //check if 2 states match each other by matching individual rules
        public bool matches(State s)
        {
            bool matches = true;
            //if input symbol is wrong then states dont match
            if(this.input != s.input)
            {
                matches = false;
            }
            else
            {
                foreach(Rule r in s.rules)
                {
                    bool rulematch = false;
                    //check for each rule
                    foreach(Rule rule in this.rules)
                    {
                        //call the method created in RULE class
                        if (rule.matches(r))
                        {
                            rulematch = true;
                            break;
                        }
                    }
                    if (!rulematch)
                    {
                        matches = false;
                        break;
                    }
                }
            }
            return matches;
        }

        //add display method
        public void displayRules(RichTextBox textBox)
        {
            foreach (Rule rul in rules)
            {
                //add -> to nonTerminal
                textBox.AppendText(rul.nonTerminal + "-> ");
                //for each item in that rule's non terminal right side add it to textBox
                foreach (string item in rul.production)
                {
                    textBox.AppendText(item + " ");
                }
                textBox.AppendText("\n");
            }
        }

        //show which inputs lead where
        public void displayOutputs(RichTextBox textBox)
        {
            //for each state in output dict add it to text box with the state name
            foreach (KeyValuePair<string, State> entry in this.outputs)
            {
                textBox.AppendText(entry.Key + " --goes to--> ");
                textBox.AppendText(entry.Value.name);
                textBox.AppendText("\n");
            }
        }

        //display to textBox
        public void displayToRichTextBox(RichTextBox textbox)
        {
            //name
            textbox.Text += "State: " + this.name + "\n";
            //where it comes from
            textbox.Text += "Input: " + this.input + "\n";


            //add all the displayes to textBox
            textbox.AppendText("\n");
            displayRules(textbox);
            textbox.AppendText("\n");
            displayOutputs(textbox);
            textbox.AppendText("\n---------------------------\n");
        }


    }
}
