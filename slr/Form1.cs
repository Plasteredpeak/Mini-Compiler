using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace slr
{
    public partial class Form1 : Form
    {
        //creat hashtables for first follow sets and production rules
        Hashtable productionRulez = new Hashtable();
        Hashtable firstSets = new Hashtable();
        public Hashtable followSets = new Hashtable();
        //start symbol
        //start symbol
        string startsymbol = "";
        //regex to check if grammar is correct
        Regex re = new Regex(@"(follow)\([A-Z]+[`]*([-][A-Z]+[`]*)*\)");
        List<Hashtable> followIterations = new List<Hashtable>();

        public Form1()
        {
            InitializeComponent();
        }

        //grid view for parsing stack
        private void Form1_Load(object sender, EventArgs e)
        {
            parsingStack.ColumnCount = 4;
            parsingStack.Columns[0].Name = "No.";
            parsingStack.Columns[1].Name = "Stack";
            parsingStack.Columns[2].Name = "Input";
            parsingStack.Columns[3].Name = "Action";
        }

        private void button1_Click(object sender, EventArgs e)
        {
            //clear previous input
            productionRulez.Clear();
            firstSets.Clear();
            followSets.Clear();
            followIterations.Clear();
            first.Clear();
            follow.Clear();

            //flag used to check for small letters non-terminals
            bool flag = true;

            //array of all the production rules
            String[] productionRules = richTextBox1.Text.Split('\n');

            //loop through all the production rules
            for (int i = 0; i < productionRules.Length; i++)
            {
                string productionRule = productionRules[i];
                //seperate the left and right part of the rule
                String[] splittedRule = productionRule.Split('>');

                //add $ to starting follow set
                if (i == 0)
                {
                    //add first non terminal to start symbol and add $ in the follow set of start symbol
                    startsymbol = splittedRule[0];
                    addToFollowSet(startsymbol, "$");
                }
                //if rule of a non terminal is not in hashtable, then put it in the hashtable
                if (!productionRulez.Contains(splittedRule[0]))
                {
                    //add rules to hashtable
                    productionRulez.Add(splittedRule[0], splittedRule[1]);
                    var te = splittedRule[0].ToCharArray()[0];
                    //check if grammar is correct according to regex
                    if (!(new Regex(@"^(([A-Z]+)|([A-Z]+[-]*[A-Z]+))[`]?$")).Match(te + "").Success)
                    {
                        flag = false;
                        MessageBox.Show("Non terminals cant be small letters");
                    }
                }
                else
                {
                    //seperate production rules by '|'
                    productionRulez[splittedRule[0]] += "|" + splittedRule[1];
                }
            }

            //if no issues if grammar
            if (flag)
            {
                foreach (DictionaryEntry rule in productionRulez)
                {
                    //for each production calculate its first set
                    getFirstSet(rule.Key.ToString());
                }

                //after caclculation first sets calculate follow set
                getFollowSet();
                checkFollow();


                //Printing First Set
                foreach (DictionaryEntry x in firstSets)
                {
                    first.AppendText("First(" + x.Key.ToString() + ") = " + "{" + cleanSet(x.Value.ToString()) + "}\n");
                }

                //generate DFA
                genDFA();

                //generate parsing stack
                generateParsingTable();

                //Sementic Rules
                sementics.Text = "" +
                        "1) E.val = E2.val + T.val\n" +
                        "2) E1.val = E2.val - T.val\n" +
                        "3) E.val = T.val\n" +
                        "4) T.val = T.val * F.val\n" +
                        "5) T.val = F.val\n" +
                        "6) F.val = E.val\n" +
                        "7) F.val = number.val";
            }


        }


        //Create List and Dictionary for DFA STATES AND DOTS
        Dictionary<string, State> DFAStatesDict = new Dictionary<string, State>();
        List<Rule> initalDots = new List<Rule>();
        List<State> dfastates = new List<State>();
        List<State> tempStates = new List<State>();


        //generate DFA
        public void genDFA()
        {
            statesBox.Text = "";
            parsingTable.Rows.Clear();

            //InitialDots list will have objects of RULE class
            initalDots = new List<Rule>();
            //states list and dictionary will have objects of STATE class
            dfastates = new List<State>();
            DFAStatesDict = new Dictionary<string, State>();
          
            //adding the first rule
            foreach (String rul in productionRulez[startsymbol].ToString().Split('|'))
            {
                //a production list that has all the rules
                List<string> production = rmNullValues(rul.Split(' ')).ToList();
                //insert dot at begining
                production.Insert(0, ".");
                //create an object of rule class using start symbol and add to initialDot list
                initalDots.Add(new Rule(startsymbol, production));
            }

            //GOTO RULE CLASS


            //adding remaining rules
            foreach (DictionaryEntry r in productionRulez)
            {
                //exclude start symbol since already been done
                if (r.Key.ToString() != startsymbol)
                {
                    //for all the rules
                    foreach (String rul in r.Value.ToString().Split('|'))
                    {
                        //split the rules by space and add them to a production list
                        List<string> production = rmNullValues(rul.Split(' ')).ToList();
                        production.Insert(0, ".");
                        //create rule object add it to initial dots
                        initalDots.Add(new Rule(r.Key.ToString(), production));
                    }
                }
            }

            //GOTO STATE CLASS

            //create a state object for state 0
            State initialState = new State("0");
            //add first rule to initial State
            initialState.addRule(initalDots[0]);
            //add generated rules to states
            addGenRules(initalDots[0], initialState);
            //execute the reductionRule method from class
            initialState.getReductionRules(followIterations.Last());
            //add initial state to DFA
            dfastates.Add(initialState);

            extendState(initialState);

           //Display all the states and add them to dictionary
            foreach (State s in dfastates)
            {
                s.displayToRichTextBox(statesBox);
                DFAStatesDict.Add(s.name, s);
            }
        }

        //generate non terminal rules after the dot
        private void addGenRules(Rule rule, State state)
        {
            for (int i = 0; i < rule.production.Count; i++)
            {
                //if we find a . in the rule
                if (rule.production[i] == ".")
                {
                    //if . not at last
                    if (i < rule.production.Count - 1)
                    {
                        string next = rule.production[i + 1];
                        //if rule goes to ~
                        if (next == "~")
                        {
                            //create a epsilon rule and if its not already present add it to state list
                            Rule emptyRule = new Rule(rule.nonTerminal, new List<string>() { "~", "." });
                            emptyRule.isComplete = true;
                            if (!ruleAlreadyThereInState(emptyRule, state))
                            {
                                state.addRule(emptyRule);
                            }
                            
                        }
                        //if dot is not at last and next is not epsilon
                        else
                        {
                            foreach (Rule r in initalDots)
                            {
                                //if there is a non Terminal in next (next is the symbol after dot)
                                if (r.nonTerminal == next)
                                {
                                    if (!ruleAlreadyThereInState(r, state))
                                    {

                                        //dont add empty rule, we already added above
                                        if (!r.production.Contains("~"))
                                        {
                                            state.addRule(r);
                                        }
                                        addGenRules(r, state);
                                    }
                                }
                            }
                        }

                    }
                }
            }

        }

        //check if a certian dot rule is present in the state
        private bool ruleAlreadyThereInState(Rule r, State s)
        {
            bool alreadyThere = false;
            foreach (Rule rule in s.rules)
            {
                if (rule.matches(r))
                {
                    alreadyThere = true;
                }
            }
            return alreadyThere;
        }

        //creates a new state based on a previous state
        private void extendState(State state)
        {
            //add all remaining states
            tempStates = new List<State>();
            foreach (Rule rule in state.rules)
            {
                for (int i = 0; i < rule.production.Count; i++)
                {
                    //if we encounter a dot in rules
                    if (rule.production[i] == ".")
                    {
                        //if dot is not at the end
                        if (i < rule.production.Count - 1)
                        {
                            //find next of dot
                            String next = rule.production[i + 1];
                            bool alreadyadded = false;

                            //move dot ahead by one point
                            foreach (State tempstate in tempStates)
                            {
                                if (tempstate.input == next)
                                {
                                    genNewRule(tempstate, rule);
                                    alreadyadded = true;
                                    break;
                                }
                            }
                            //if state is not already added to the dictionary
                            if (!alreadyadded)
                            {
                                //create a new state
                                State newState = new State("");

                                //add it to the temp state list
                                newState.input = next;
                                genNewRule(newState, rule);
                                tempStates.Add(newState);
                            }

                        }
                        break;
                    }
                }
            }
            foreach (State newstate in tempStates)
            {
                Boolean statealreadythere = false;
                foreach (State dfastate in dfastates)
                {
                    if (dfastate.matches(newstate)) {
                        state.outputs.Add(dfastate.input, dfastate);
                        statealreadythere = true;
                        break;
                    }
                }
                if (!statealreadythere)
                {
                    newstate.getReductionRules(followIterations.Last());
                    newstate.name = ""+dfastates.Count;
                    state.outputs.Add(newstate.input, newstate);
                    dfastates.Add(newstate);
                    //if that state not extended
                    if (!newstate.extended)
                    {
                        extendState(newstate);
                        newstate.extended = true;
                    }

                }
            }
        }
    
        //move dot to right side of the rule and add it to the newly created state
        private void genNewRule(State state, Rule rule) {
            Rule newr = rule.copy();
            int index = rule.production.LastIndexOf(".");
            newr.production.Remove(".");
            newr.production.Insert(index + 1, ".");
            state.addRule(newr);
            addGenRules(newr, state);
        }




        //method generates the parsing table in data grid view
        private void generateParsingTable()
        {
            //create a terminal and non terminal list
            List<string> nonTerminals = new List<string>();
            List<string> Terminals = new List<string>();

            foreach (Rule rule in initalDots)
            {
                foreach (string symbol in rule.production)
                {
                    if (symbol != ".")
                    {
                        // add nonterminals to the list
                        if (productionRulez.Contains(symbol))
                        {
                            if (!nonTerminals.Contains(symbol))
                            {
                                nonTerminals.Add(symbol);
                            }
                        }
                        //add terminals to the list
                        else
                        {
                            if (!Terminals.Contains(symbol))
                            {
                                Terminals.Add(symbol);
                            }
                        }
                    }
                }
            }


            parsingTable.AutoResizeColumns();
            
            parsingTable.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells;
            //colums for grid +2 for dollar sign and State
            int colcount = nonTerminals.Count + Terminals.Count + 2;
            parsingTable.ColumnCount = colcount;

            //first column will be State
            parsingTable.Columns[0].Name = "State";

            //add names for all the terminals except epsilon
            for (int i = 0; i < Terminals.Count; i++)
            {
                if(Terminals[i] != "~")
                {
                    parsingTable.Columns[i + 1].Name = Terminals[i];
                }
            }
            //add dollar at the end of terminals
            parsingTable.Columns[Terminals.Count+1].Name = "$";

            //add names for the nonTerminals
            for (int i = 0; i < nonTerminals.Count; i++)
            {
                parsingTable.Columns[i + Terminals.Count + 2].Name = nonTerminals[i];
            }
            //loop through all the dfa satetes
            foreach (State state in dfastates)
            {
                //add row to gridView
                List<string> row = new List<string>();
                //for each State one row will be added
                row.Add(state.name);

                //fill terminals in the parsing table
                foreach (string t in Terminals)
                {
                    if(t != "~")
                    {
                        //if that state goes to a certian terminal add it to the row
                        if (state.outputs.ContainsKey(t))
                        {
                            row.Add("S"+state.outputs[t].name);
                        }
                        //if the terminal is in a reductionrule then reduce using that rule
                        else if (state.reductionRules.ContainsKey(t))
                        {
                            row.Add("R(" + state.reductionRules[t].convertString()+")");
                        }
                        //if it does not exist add nothing to that terminal
                        else
                        {
                            row.Add("");
                        }
                    }
                   
                }


                //fill $;
                if (state.reductionRules.ContainsKey("$"))
                {
                    //for start symbol add accept to the $ column
                    if (state.reductionRules["$"].nonTerminal == startsymbol)
                    {
                        row.Add("accept");
                    }
                    else
                    {
                        row.Add("R(" + state.reductionRules["$"].convertString()+")");
                    }
                    
                }
                else
                {
                    row.Add("");
                }

                //fill nonTerminals of parsing stack
                foreach (string nt in nonTerminals)
                {
                    //if output consist of a non terminal then write that State in the non terminal column
                    if (state.outputs.ContainsKey(nt))
                    {
                        row.Add(state.outputs[nt].name);
                    }
                    else
                    {
                        row.Add("");
                    }
                }
                parsingTable.Rows.Add(row.ToArray());

            }

            

        }


        //for input
        private void button2_Click(object sender, EventArgs e)
        {
           
            string input = richTextBox2.Text;
            parseInput(input);

            //SDT
            string nospace = new string(input.ToCharArray().Where(c => !Char.IsWhiteSpace(c)).ToArray());
            nospace = nospace.Substring(0, nospace.Length - 1);


            char[] chars = nospace.ToCharArray();
            List<NonTerminal> list = new List<NonTerminal>();

            string state = "exp";
            NonTerminal ro = ParseCase1(state, chars, 0, chars.Length - 1);
            if (ro.val != int.MinValue)
            {
                Output.Text += "exp.val = " + ro.val + "\n";
            }
            else
            {
                Output.Text += "ERROR" + "\n";
            }
            //--------------

            //Tokenization

            tokens.Columns.Clear();
            tokens.Rows.Clear();
            tokens.ColumnCount = 2;
            tokens.Columns[0].Name = "TYPE";
            tokens.Columns[1].Name = "VALUE";
            tokens.AutoResizeColumns();
            tokens.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells;

            tokenization(nospace);

            string codeInput = "(" + nospace + ")";


            //Three Address Code
            List<Instruction> instructions = Instruction.GenerateAddressCode(codeInput);
            

            //Quadruples
            quad.Columns.Clear();
            quad.Rows.Clear();
            quad.ColumnCount = 4;
            quad.Columns[0].Name = "OP";
            quad.Columns[1].Name = "Arg1";
            quad.Columns[2].Name = "Arg2";
            quad.Columns[3].Name = "Dest.";
            quad.AutoResizeColumns();
            quad.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells;

            //Triples
            triple.Columns.Clear();
            triple.Rows.Clear();
            triple.ColumnCount = 3;
            triple.Columns[0].Name = "OP";
            triple.Columns[1].Name = "Arg1";
            triple.Columns[2].Name = "Arg2";
            triple.AutoResizeColumns();
            triple.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells;



            // Print the three-address code instructions
            foreach (Instruction i in instructions)
            {
                //Three Address
                code.Text += (i.Result + " = " + i.Arg1 + " " + i.Op + " " + i.Arg2 + "\n");

                //Quadruples
                quad.Rows.Add(i.Op, i.Arg1, i.Arg2, i.Result);

                //triples
                triple.Rows.Add(i.Op, i.Arg1, i.Arg2);
            }




        }

        public void tokenization(string input)
        {
            Regex integer = new Regex(@"^[0-9]+$");
            Regex op = new Regex(@"^[\+\-\*\(\)]$");

            foreach(char c in input)
            {
                if(integer.Match(c + "").Success)
                {
                    tokens.Rows.Add("integer",c );
                }
                if (op.Match(c + "").Success)
                {
                    tokens.Rows.Add("operator", c);
                }
            }
        }
        public struct NonTerminal
        {
            public long val;
            public int basee;
            public string dtype;
            public NonTerminal(int v)
            {
                val = int.MinValue;
                basee = int.MinValue;
                dtype = null;
            }
        }

        public NonTerminal ParseCase1(string state, char[] chars, int start, int end)
        {
            NonTerminal root = new NonTerminal(0);
            if (state == "exp")
            {
                bool found = false;
                int started = 0;
                for (int i = end; i >= start; i--)
                {
                    if (chars[i] == '(')
                    {
                        started--;
                    }
                    else if (chars[i] == ')')
                    {
                        started++;
                    }
                    if ((chars[i] == '+' || chars[i] == '-') && (started == 0))
                    {

                        found = true;
                        NonTerminal left = ParseCase1("exp", chars, start, i - 1);
                        Output.Text += "E.val = " + left.val + "\n";
                        NonTerminal right = ParseCase1("term", chars, i + 1, end);
                        Output.Text += "E.val = " + right.val + "\n";
                        if (chars[i] == '+')
                        {
                            root.val = (left.val + right.val);
                        }
                        else
                        {
                            root.val = (left.val - right.val);
                        }
                        break;
                    }

                }
                if (!found)
                {
                    NonTerminal term = ParseCase1("term", chars, start, end);
                    Output.Text += "T.val = " + term.val + "\n";
                    root.val = term.val;
                }
            }
            else if (state == "term")
            {
                bool found = false;
                int started = 0;
                for (int i = end; i >= start; i--)
                {
                    if (chars[i] == '(')
                    {
                        started--;
                    }
                    else if (chars[i] == ')')
                    {
                        started++;
                    }
                    if ((chars[i] == '*') && (started == 0))
                    {
                        found = true;
                        NonTerminal left = ParseCase1("term", chars, start, i - 1);
                        Output.Text += "T.val = " + left.val + "\n";
                        NonTerminal right = ParseCase1("factor", chars, i + 1, end);
                        Output.Text += "F.val = " + right.val + "\n";

                        root.val = (left.val * right.val);
                        break;
                    }

                }
                if (!found)
                {
                    NonTerminal factor = ParseCase1("factor", chars, start, end);
                    Output.Text += "F.val = " + factor.val + "\n";
                    root.val = factor.val;
                }
            }
            else if (state == "factor")
            {
                Regex num_reg = new Regex("^[0-9]+$");
                int stop = (end + 1) - start;
                try
                {
                    String str = new string(chars).Substring(start, stop);
                    if (num_reg.IsMatch(str))
                    {
                        NonTerminal number = ParseCase1("number", chars, start, end);
                        Output.Text += "N.inval = " + number.val + "\n";
                        root.val = number.val;
                    }
                    else
                    {
                        NonTerminal exp = ParseCase1("exp", chars, start + 1, end - 1);
                        Output.Text += "E.val = " + exp.val + "\n";
                        root.val = exp.val;
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    MessageBox.Show("Incorrect Input", "ERROR",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return root;
                }

            }
            else if (state == "number")
            {
                string str = new string(chars);
                int stop = (end + 1) - start;
                int val = int.Parse(str.Substring(start, stop));
                root.val = val;
            }
            return root;
        }

        //parsing input
        private void parseInput(string inp)
        {
            //clear previous input
            parsingStack.Rows.Clear();
            parsingStack.AutoResizeColumns();
            parsingStack.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells;
            List<string> stack = new List<string>();
            List<string> input = new List<string>();
            //split each input item and add it to the input list
            foreach(string item in inp.Split(' '))
            {
                if(item!=" " && item != "" && item != "")
                {
                    input.Add(item);
                }
            }
            //add state 0 and dollar in stack
            stack.Add("$");
            stack.Add("0");


            bool accepted = false;
            int iterationNo = 1;
            //loop through till not accepted or no error
            while (!accepted)
            {
                //row to be added to gridView
                List<string> parsingStackRow = new List<string>();

                //last state of DFA
                State lastState = DFAStatesDict[stack.Last()];

                //add everything to gridView
                parsingStackRow.Add(iterationNo+"");
                parsingStackRow.Add(getListInString(stack));
                parsingStackRow.Add(getListInString(input));

                //shift case if outputs contains input
                if (lastState.outputs.ContainsKey(input.First()))
                {
                    parsingStackRow.Add("shift " + lastState.outputs[input.First()].name);
                    stack.Add(input.First());
                    //add State name
                    stack.Add(lastState.outputs[input.First()].name);
                    //add input
                    input = shiftInput(input);
                }

                //reduce case if laststate is in reductionrulels
                else if (lastState.reductionRules.ContainsKey(input.First()))
                {
                    Rule reductionRule = lastState.reductionRules[input.First()];

                    if (reductionRule.production[0] == "~")
                    {
                        //add the nonterminal which was used to reduce
                        stack.Add(reductionRule.nonTerminal);
                        //name of the state which had the reduction Rule
                        stack.Add(DFAStatesDict[lastState.name].outputs[reductionRule.nonTerminal].name);
                     //add the rule which was used to reduce
                        parsingStackRow.Add("r (" + reductionRule.convertString() + ")");

                    }
                    else
                    {
                        int stackindex = stack.LastIndexOf(reductionRule.production[0]);
                        bool matchingStack = true;
                        for (int i = 0; i < reductionRule.production.Count - 1; i++)
                        {
                            if (reductionRule.production[i] != stack[stackindex])
                            {
                                matchingStack = false;
                                break;
                            }
                            stackindex += 2;
                        }
                        //if reduction can be done
                        if (matchingStack)
                        {
                            //if startsymbol is gotten after reduction
                            if (reductionRule.nonTerminal == startsymbol )
                            {
                                parsingStackRow.Add("Accept");
                                parsingStack.Rows.Add(parsingStackRow.ToArray());
                                accepted = true;
                                break;
                            }
                            //add the reduce rule
                            parsingStackRow.Add("r (" + reductionRule.convertString() + ")");

                            List<string> newstack = new List<string>();
                            for (int i = 0; i < stack.LastIndexOf(reductionRule.production[0]); i++)
                            {
                                newstack.Add(stack[i]);
                            }
                            //add the state of the next input item
                            string laststateofnew = newstack.Last();
                            newstack.Add(reductionRule.nonTerminal);
                            newstack.Add(DFAStatesDict[laststateofnew].outputs[reductionRule.nonTerminal].name);
                            stack = newstack;
                        }
                        //if nothing is found in the parsing table
                        else{
                            parsingStack.Rows.Add("ERROR");
                            Console.Write("error in matching");
                            break;
                        }
                    }
                }
                else
                {
                    parsingStack.Rows.Add("ERROR");
                    Console.WriteLine("error");
                    break;

                }
                //add rows increase iteration
                parsingStack.Rows.Add(parsingStackRow.ToArray());
                iterationNo++;
            }
        }

    
        //Add input to the stack
        private List<string> shiftInput(List<string> input)
        {
            List<string> newlist = new List<string>();
            for(int i = 1; i < input.Count; i++)
            {
                newlist.Add(input[i]);
            }
            return newlist;
        }

        //return the list as a string to display in gridview
        private string getListInString(List<string> list)
        {
            string str = "";
            foreach (string item in list)
            {
                str+=item + " ";
            }
            return str;
        }

  


        //first and follow sets
        private void getFirstSet(string nonTerminal)
        {
            List<String[]> rules = new List<String[]>();
            //split rules again based on non terminal
            foreach (String rul in productionRulez[nonTerminal].ToString().Split('|'))
            {
                rules.Add(rmNullValues(rul.Split(' ')));
            }
            foreach (String[] rul in rules)
            {
                //for recursion
                if (rul[0] != nonTerminal)
                {
                    //Terminal case //B
                    if (!firstSets.Contains(nonTerminal))
                    {
                        firstSets.Add(nonTerminal, cleanSet(calculateFirst(nonTerminal, rul, 0)));
                    }
                    else
                    {
                        firstSets[nonTerminal] += "," + cleanSet(calculateFirst(nonTerminal, rul, 0));
                    }
                }
            }
        }


        private string calculateFirst(string nonterminal, String[] rule, int index)
        {
            //if it is a terminal
            if (!productionRulez.Contains(rule[0]) && rule[0] != "~")
            {
                return rule[0];
            }
            //case of non-terminal
            else if (rule[0] != "~" && rule.Length >= 1 && index < rule.Length)
            {
                //calculate first of that non terminal
                string fsOfNt = nonTerminalCase(rule[index]); //S> A B c 

                if (fsOfNt.Contains("~"))
                {
                    //if first of that non terminal contains epsilon then calculate first of next as well
                    return fsOfNt + calculateFirst(nonterminal, rule, index + 1);
                }

                else
                {
                    return fsOfNt;
                }
            }

            return "~";
        }



        private string nonTerminalCase(string nonTerminal)
        {

            List<String[]> rules = new List<String[]>();

            //calculate first for nonterminal within a first Set
            foreach (String rul in productionRulez[nonTerminal].ToString().Split('|'))
            {
                rules.Add(rmNullValues(rul.Split(' ')));
            }

            string firstSet = "";

            foreach (String[] rul in rules)
            {
                if (rul[0] != nonTerminal)
                {
                    string fs = calculateFirst(nonTerminal, rul, 0);
                    firstSet += fs + ",";
                }
            }
            return firstSet;
        }


        //clean the grammar remove null values or unnecessary spaces
        private String[] rmNullValues(string[] array)
        {
            var temp = new List<string>();
            foreach (string s in array)
            {
                if (!string.IsNullOrEmpty(s))
                    temp.Add(s);
            }
            return temp.ToArray();
        }


        //removing extra commas and repeated terminals from first set
        private string cleanSet(string firstset)
        {
            string finalFirstSet = "";
            String[] temparr = firstset.Split(',');

            for (int i = 0; i < temparr.Length; i++)
            {
                string item = temparr[i];
                if (item != " " && item != "" && !finalFirstSet.Contains(item))
                {
                    if (i != 0)
                    {
                        finalFirstSet += ",";
                    }
                    finalFirstSet += item;
                }
            }
            return finalFirstSet;
        }


        //calculate follow set after first sets are calculated
        private void getFollowSet()
        {
            foreach (DictionaryEntry r in productionRulez)
            {
                List<String[]> rules = new List<String[]>();
                //split rules by spaces and remove null spaces
                foreach (String rul in r.Value.ToString().Split('|'))
                {
                    rules.Add(rmNullValues(rul.Split(' ')));
                }

                foreach (string[] rule in rules)
                {

                    for (int i = 0; i < rule.Length; i++)
                    {
                        string entry = rule[i];
                        if (productionRulez.Contains(entry))
                        {
                            //non terminal detected in a production rule
                            if (i == rule.Length - 1 && entry != r.Key.ToString())
                            {
                                addToFollowSet(entry, "follow(" + r.Key.ToString() + ")");
                            }
                            
                            else
                            {
                             
                                //terminal on right side of non-terminal
                                if (i + 1 < rule.Length)
                                {
                                    //simply add terminal to follow set
                                    if (!productionRulez.Contains(rule[i + 1]))
                                    {
                                        addToFollowSet(entry, rule[i + 1]);
                                    }

                                    //non-terminal on right side of non-terminal
                                    else
                                    {
                                        //get firstSet of that non terminal
                                        string firstSet = firstSets[rule[i + 1]].ToString();
                                        //if first empty, then follow of that nonterminal is follow of this
                                        if (firstSet == "~" && entry != r.Key.ToString())
                                        {
                                            addToFollowSet(entry, "follow(" + r + ")");
                                        }
                                       
                                        //if first not empty, then add first of non terminal to follow of this
                                        else
                                        {
                                            string[] firstSetValues = firstSet.Split(',');
                                            foreach (string value in firstSetValues)
                                            {
                                                if (value != "~")
                                                {
                                                    addToFollowSet(entry, value);
                                                }
                                            }
                                        }

                                    }
                                }
                            }

                        }
                    }

                }

            }
        }


        private void checkFollow()
        {

            followIterations.Add(followSets);

            for (int i = 1; i <= followSets.Count; i++)
            {
                followIterations.Add(new Hashtable());
                for (int j = 0; j < 10; j++)
                {
                    UpdateFollow(followIterations[i - 1], followIterations[i]);
                }
            }
            foreach (DictionaryEntry x in followIterations[followSets.Count])
            {
                follow.AppendText("Follow(" + x.Key.ToString() + ") = " + "{" + cleanSet(x.Value.ToString()) + "}\n");
            }


        }

        private void UpdateFollow(Hashtable followSets, Hashtable newfollow)
        {
            foreach (DictionaryEntry x in followSets)
            {

                string fs = x.Value.ToString();

                newfollow[x.Key] = fs;

                var match = re.Match(fs);

                if (match.Success)
                {
                    int totalmatches = match.Groups.Count;
                    for (int j = 0; j < totalmatches; j++)
                    {
                        string nonterminal = (fs.Split('(')[1].Split(')')[0]);
                        if (newfollow[nonterminal] == null)
                        {
                            string newfollowset = followSets[nonterminal].ToString();
                            string updatedFollow = x.Value.ToString().Replace("follow(" + nonterminal + ")", newfollowset);

                            if (updatedFollow.Contains(x.Key.ToString()))
                            {
                                updatedFollow = updatedFollow.Replace("follow(" + nonterminal + ")", "");
                            }
                            newfollow[x.Key] = updatedFollow;
                        }
                        else
                        {
                            string newfollowset = newfollow[nonterminal].ToString();
                            string updatedFollow = x.Value.ToString().Replace("follow(" + nonterminal + ")", newfollowset);

                            if (updatedFollow.Contains(x.Key.ToString()))
                            {
                                updatedFollow = updatedFollow.Replace("follow(" + nonterminal + ")", "");
                            }

                            newfollow[x.Key] = updatedFollow;
                        }


                    }

                }

               
            }
            

        }
        //add follow sets
        private void addToFollowSet(string nonTerminal, string value)
        {
            //if non terminal
            if (followSets.Contains(nonTerminal))
            {
                followSets[nonTerminal] += "," + value;
            }
            //if terminal then simply add
            else
            {
                followSets.Add(nonTerminal, value);
            }
        }

        private void first_TextChanged(object sender, EventArgs e)
        {

        }
    }
    public class Instruction
    {
        public string Op { get; set; }
        public string Arg1 { get; set; }
        public string Arg2 { get; set; }
        public string Result { get; set; }

        public Instruction(string op, string arg1, string arg2, string result)
        {
            Op = op;
            Arg1 = arg1;
            Arg2 = arg2;
            Result = result;
        }

        public static List<Instruction> GenerateAddressCode(String input)
        {


            // Use a stack to store operands and operator
            Stack<string> operandStack = new Stack<string>();
            Stack<char> operatorStack = new Stack<char>();

            // Create a list of three-address code instructions
            List<Instruction> instructions = new List<Instruction>();

            // Parse the input string and generate three-address code instructions
            for (int i = 0; i < input.Length; i++)
            {
                char c = input[i];

                // If the character is an operand, push it onto the operand stack

                if (char.IsDigit(c))
                {
                    operandStack.Push(c.ToString());
                }

                if (char.IsLetter(c))
                {
                    operandStack.Push(c.ToString());
                }


                // If the character is an operator, push it onto the operator stack
                else if (c == '+' || c == '-' || c == '*' || c == '/' || c == '=')
                {
                    operatorStack.Push(c);
                }
                // If the character is a parenthesis, pop the top two operands and the top operator
                // and generate three-address code instructions for the operation
                else if (c == ')')
                {
                    string op2 = operandStack.Pop();
                    string op1 = operandStack.Pop();
                    char op = operatorStack.Pop();

                    // Generate a unique variable name for the result
                    string resultVar = "t" + instructions.Count;

                    // Add a three-address code instruction to the list
                    instructions.Add(new Instruction(op.ToString(), op1, op2, resultVar));

                    // Push the result back onto the operand stack
                    operandStack.Push(resultVar);
                }
            }

            // The final result is the top element on the operand stack
            string finalResult = operandStack.Pop();



            return instructions;
        }
    }
}
