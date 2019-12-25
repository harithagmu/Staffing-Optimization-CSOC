# Staffing-Optimization-CSOS
C#, MYSQL
### Design document – Vulnerability analysis
#### Objective:
To minimize the number of employees, subject to all the constraints listed under assumptions
#### Assumptions:
•	All the tasks coming on a day should be started working on the same day irrespective of the priority
•	30 requests(tasks) are processed per day
•	A task should be finished on or before the deadline
•	Analysts hired for a team are always allocated to work for the same team. For example, analyst4 hired by team1 contributes to team 1’s effort only.
•	Effort is measured in man days only. 
•	Application does not consider additional days required for training the analysts
•	Initially all teams start with 0 employees and based on effort requirement the analysts are hired.
•	For sequential the tasks are always processed in the order Team 1 followed by Team2 followed by Team 3.
•	No employees are fired until the completion of this project
About the data:
What data is available / what data can application process?
•	There are currently 3 teams operating in the firm to work on vulnerability processing
•	Each task has:
o	Task ID  - Unique ID for each task
o	Effort – Number of days of effort required from at least one of the three teams.
This is measured in man days.
o	Priority level – Critical, High, Medium, Low
o	SLA – Number of days to finish the task
o	Task type 
	Concurrent – All the teams start simultaneously
	Sequential – Teams work one after the other(assume waterfall model)
o	Start day – Day on which the task should be started

#### Processing:
•	Reads and stores the set of tasks per day to process in the database.
•	Marks the status of tasks received on that day as ‘New’.
•	Starts hiring the analysts required based on the effort and deadline. Firstly, checks if any analysts are free and are available to be assigned to the new tasks. Otherwise,
o	For parallel tasks:
	If effort from all the teams < deadline then one analyst per team is hired.
	If the effort from any team > deadline then analysts are hired based on fraction of work. For E.g.: If the SLA = 3 and effort from team1 = 7 then 7/3 rounded up to 3 analysts are hired for team1.
o	For Sequential tasks:
	If the total effort(sum of effort) from all the teams < deadline then one analyst per team is hired.
	If the total effort > deadline then number of analysts per team hired are different for different priority levels of tasks:
	For critical task where SLA = 3, number of analysts hired = number of man-days or the effort required.
	For any other priority level (high, medium and low) the number of analysts hired for a team k =(effortk / total effort) * SLA where effortk is the effort required by team k. To this value, the algorithm applies rounding up or rounding down by comparing total effort with deadline.
For e.g. if effort 1 = 3, effort 2 = 2, effort 3 = 4 and SLA = 7 then,
	Analysts from team 1 = (3/9)*7 = 2.34 => 2
      Analysts from team 2 = (2/9)*7 = 1.56 => 1
      Analysts from team 3 = (4/9)*7 = 3.11 => 3
If the total effort is still greater than the deadline then the above values are rounded up to next integer. In which analysts hired for team1, team2 and team3 are 3,2,4 respectively. But for this case, the program applies rounding down the above values.
•	Once the analysts are hired, status of the task changes to in-progress.
•	For each in progress task, the effort is tracked by the number of analysts assigned to that task. New effort = effort – analysts. 
•	To handle the case where number of analysts available is greater than effort required, the additional analysts are released and made available in the free pool to be ready for next task assignment. 
Consider the case where a parallel task has team 1 effort = 7 and SLA = 3 and from the allocation processing step, we know that (7/3) rounded up to 3 analysts are hired. All 3 analysts working 2 days will finish 6/7th of the task and 3rd day requires just one-man day. So, the two additional analysts working on that task will be released on the day 3 of the task.

