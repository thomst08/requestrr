import { useEffect, useState } from "react";
import {
  Button,
  FormGroup,
  Input,
  Label,
  Row,
  Col,
  Card,
  CardBody,
  Alert,
} from "reactstrap";

/**
 * Lets the operator manage extra Overseerr / Jellyseerr instances on top of the
 * primary connection. Each extra instance has a unique numeric InstanceId and
 * can be picked per-category.
 *
 * Props:
 *   - instances: current instances array
 *   - onChange: (newInstances) => void
 */
function OverseerrInstancesEditor(props) {
  const instances = props.instances || [];
  const [error, setError] = useState("");

  const nextInstanceId = () => {
    let id = 1;
    const taken = new Set(instances.map(i => Number(i.instanceId)));
    while (taken.has(id)) id++;
    return id;
  };

  const addInstance = () => {
    const id = nextInstanceId();
    props.onChange([
      ...instances,
      {
        instanceId: id,
        name: `Instance ${id}`,
        hostname: "",
        port: 5055,
        useSSL: false,
        apiKey: "",
        version: "1",
      }
    ]);
  };

  const removeInstance = (instanceId) => {
    props.onChange(instances.filter(i => i.instanceId !== instanceId));
  };

  const updateInstance = (instanceId, field, value) => {
    props.onChange(instances.map(i => {
      if (i.instanceId !== instanceId) return i;
      return { ...i, [field]: value };
    }));
  };

  useEffect(() => {
    const ids = instances.map(i => Number(i.instanceId));
    if (new Set(ids).size !== ids.length) {
      setError("Each extra instance needs a unique id.");
    } else if (instances.some(i => !i.hostname || !i.apiKey)) {
      setError("Every extra instance needs a hostname and API key.");
    } else {
      setError("");
    }
  }, [JSON.stringify(instances)]);

  return (
    <>
      <div className="d-flex align-items-center justify-content-between">
        <h6 className="heading-small text-muted mb-0">
          Additional Overseerr / Jellyseerr Instances
          <span className="crios-badge ml-2">CRIOS</span>
        </h6>
        <Button color="default" size="sm" type="button" onClick={addInstance}>
          <i className="fas fa-plus mr-1"></i>
          Add Instance
        </Button>
      </div>
      <div className="pl-lg-4 mt-3">
        <p className="text-muted small mb-3">
          The primary connection above is always instance <code>0</code>. Add extra
          instances here and pick one per category to dispatch requests to a
          different Overseerr or Jellyseerr server.
        </p>
        {error ? (
          <Alert color="warning" className="text-center mb-3">
            <strong>{error}</strong>
          </Alert>
        ) : null}
        {instances.length === 0 ? (
          <Alert color="info" className="mb-0">
            No additional instances. Add one to route specific categories
            (e.g. 4K, anime, music) to a different server.
          </Alert>
        ) : (
          instances.map((instance) => (
            <Card key={instance.instanceId} className="mb-3">
              <CardBody>
                <Row>
                  <Col lg="4">
                    <FormGroup>
                      <Label>Display Name</Label>
                      <Input
                        type="text"
                        placeholder="e.g. CRIOS Media"
                        value={instance.name || ""}
                        onChange={e => updateInstance(instance.instanceId, "name", e.target.value)}
                      />
                    </FormGroup>
                  </Col>
                  <Col lg="2">
                    <FormGroup>
                      <Label>Instance ID</Label>
                      <Input
                        type="number"
                        min="1"
                        value={instance.instanceId}
                        readOnly
                      />
                    </FormGroup>
                  </Col>
                  <Col lg="3">
                    <FormGroup>
                      <Label>Hostname</Label>
                      <Input
                        type="text"
                        placeholder="req.example.com"
                        value={instance.hostname || ""}
                        onChange={e => updateInstance(instance.instanceId, "hostname", e.target.value)}
                      />
                    </FormGroup>
                  </Col>
                  <Col lg="2">
                    <FormGroup>
                      <Label>Port</Label>
                      <Input
                        type="number"
                        min="1"
                        max="65535"
                        value={instance.port || 5055}
                        onChange={e => updateInstance(instance.instanceId, "port", Number(e.target.value))}
                      />
                    </FormGroup>
                  </Col>
                  <Col lg="1" className="d-flex align-items-end">
                    <FormGroup className="custom-control custom-control-alternative custom-checkbox mb-3">
                      <Input
                        className="custom-control-input"
                        id={`useSSL-instance-${instance.instanceId}`}
                        type="checkbox"
                        checked={!!instance.useSSL}
                        onChange={() => updateInstance(instance.instanceId, "useSSL", !instance.useSSL)}
                      />
                      <Label
                        className="custom-control-label"
                        htmlFor={`useSSL-instance-${instance.instanceId}`}>
                        <span className="text-muted">SSL</span>
                      </Label>
                    </FormGroup>
                  </Col>
                </Row>
                <Row>
                  <Col lg="9">
                    <FormGroup>
                      <Label>API Key</Label>
                      <Input
                        type="text"
                        placeholder="API key for this instance"
                        value={instance.apiKey || ""}
                        onChange={e => updateInstance(instance.instanceId, "apiKey", e.target.value)}
                      />
                    </FormGroup>
                  </Col>
                  <Col lg="3" className="text-right d-flex align-items-end justify-content-end">
                    <Button
                      color="danger"
                      size="sm"
                      type="button"
                      onClick={() => removeInstance(instance.instanceId)}
                      className="mb-3"
                    >
                      <i className="fas fa-trash mr-1"></i>
                      Remove
                    </Button>
                  </Col>
                </Row>
              </CardBody>
            </Card>
          ))
        )}
      </div>
      <hr className="my-4" />
    </>
  );
}

export default OverseerrInstancesEditor;
