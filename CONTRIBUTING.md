# Contributing to OpenPerpetuumServer

We generally follow a simplified gitflow model, with some coupling to other repository states, like the OPDB.

To contribute:
 - Branch from the most recent Development
 - Commit your code, push and submit a Pull Request to Development

Your code will be reviewed and merged if it does the following.
Either:
 - Addresses or implements an OP-Project identified bug, feature, or change.
 - Enhances, optimizes, or extends existing functionality.
 - Refactors that improves maintainability  of the codebase.

Your code must:
 - Not break style conventions within the codebase.
 - Be neither ungrokable, insufficiently documented (self-/comments) nor otherwise unmaintainable by other experienced developers.
 - Not introduce, change, or manipulate game mechanics outside the scope of OP-Project issue cards.
 - Not introduce, exploit, or otherwise attempt to introduce vulnerabilities to compromise game integrity or user data.

Reviewers will point out when these tenets are not followed, but it will save everyone time if you adhere to these rules the first time.  

Special Notes:
The PerpetuumServer is highly coupled in some instances to DB state.  The OPDB has its own patching source control model which you should learn and follow.  If your pull request also requires database changes:
 - Submit your PR to the server, and another PR of your SQL patch to the OPDB development branch
 - **Include a comment in the PR note that the server and DB patch are interdependent**

## OP-project - Cross Repo issue and feature planning
As mentioned above, all action items are collected in one kanban board for Server related development.  These issues should be pointed to with all PR's made.  A PR should address at most one OP-Project issue.  When a server change requires a DB change, use the issue too as a mechanism to indicate the coupling.

## Code of Conduct

Your code, comments, commit messages, and everything you do on the internet will reflect on you and how others perceive you.  Act accordingly.
Trolling, harassment or abusive behaviour of any kind will not be tolerated.
This is an open source, volunteer project.  Respect people's time and privacy.

## The Team

We have a github group (fancy!) which is a subset of our full project team and includes all of our active contributing development team members and some design members.
To join, hop on the discord, or find our volunteer survey, and speak with a member of the team.

Warning: we do have standards.  You will have to actually have to do something to get on the team.  
Reading this is the first step, let us know you managed to get this far.