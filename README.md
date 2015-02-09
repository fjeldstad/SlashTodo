when commandBus.send(new AddTask(userId: "anders", listId: "x", taskText: "get coffee"));
then eventBus.raise(new TaskAdded(by: "anders", listId: "x", taskId: "y", "taskText": "get coffee"))