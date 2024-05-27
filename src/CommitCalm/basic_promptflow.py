import json

from pathlib import Path
folder = Path(__file__).parent.absolute().as_posix()

from promptflow.core import tool, Prompty

@tool
def flow_entry(    
      topic: any
) -> str:
  # path to prompty (requires absolute path for deployment)
  path_to_prompty = folder + "/basic.prompty"

  # load prompty as a flow
  flow = Prompty.load(path_to_prompty)
 
  # execute the flow as function
  result = flow(
    topic = topic
  )

  return result

if __name__ == "__main__":
   json_input = '''{
  "topic": "Gratitude"
}'''
   args = json.loads(json_input)

   result = flow_entry(**args)
   print(result)
