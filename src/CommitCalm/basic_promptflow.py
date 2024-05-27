import json
import argparse

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
   parser = argparse.ArgumentParser()
   parser.add_argument("--topic", default="Gratitude", help="Topic for the flow")
   args = parser.parse_args()

   result = flow_entry(topic=args.topic)
   print(result)
