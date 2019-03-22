# 02285 - AI and MAS - Programming Project

We solve box problems.

Remember we are measured in 1) Optimal solution length and 2) Total computation time.
That also means memory/space reductions is less a priority.

Strategies (feel free to add):
* Only save states of significance (i.e. checkpoints), skip intermediate steps. Saves mostly only memory.

Idea for structure to be discussed:

1) Load level in.

2) Split level with level splitter.

Do for each split level:

3) Calculate the order the goals have to be solved in(Many can be done at the same time)

	a) Maybe also calculate spots where the agent can end(to not fuck up later goals) after solving a specific goal. 

b) Even for Single Agent, it is sometimes possible to solve multiple goals at the same time. In this case we should later decide how solving those is the fastest, i.e. both at the same time or one before the other.

Do for each goal in the correct order. With something to handle if mutiple goals at the same time. Do this seperatly for each agent.

4) Calculate which boxes has to be moved to solve the first goal for each agent, and where they should be moved.
	4a) This could also just be done in the A*/greedy with a heuristic
	4b) Do A*/greedy on the subgoals of moving boxes out of the way, and the correct box to the goal.

5) Merge the plans for individual agents, and go back to step 4 for the next goals, until level is solved.

6) Merge the plans for split levels, and output this to the server
