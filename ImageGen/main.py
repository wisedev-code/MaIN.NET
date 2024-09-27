import torch
from diffusers import FluxPipeline
from flask import Flask, jsonify, request, send_file
from flasgger import Swagger, swag_from
from io import BytesIO

app = Flask(__name__)
swagger = Swagger(app)

pipe = FluxPipeline.from_pretrained("black-forest-labs/FLUX.1-schnell", torch_dtype=torch.float16)
pipe.enable_sequential_cpu_offload()

@app.route('/health', methods=['GET'])
@swag_from({
    'parameters': [],
    'responses': {
        200: 'OK'
    }
})
def health():
    return 'OK'

@app.route('/generate/<string:prompt>', methods=['POST'])
@swag_from({
    'parameters': [
        {
            'name': 'prompt',
            'in': 'path',
            'type': 'string',
            'required': True,
            'description': 'Text prompt to generate an image'
        }
    ],
    'responses': {
        200: {
            'description': 'Generated image from the prompt',
            'content': {
                'image/png': {
                    'schema': {
                        'type': 'string',
                        'format': 'binary'
                    }
                }
            }
        }
    }
})
def generate(prompt):
    # Generate image using the provided prompt
    image = pipe(
        prompt,
        guidance_scale=0.0,
        num_inference_steps=4,
        max_sequence_length=256
    ).images[0]

    # Save the image to an in-memory buffer
    img_io = BytesIO()
    image.save(img_io, 'PNG')
    img_io.seek(0)

    # Return the image as a response
    return send_file(img_io, mimetype='image/png')

if __name__ == '__main__':
    app.run(host="0.0.0.0", port=5003)